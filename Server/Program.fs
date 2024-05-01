namespace ImaginaryAlchemy

open System

open Suave
open Suave.Logging
open Suave.Operators

open Fable.Remoting.Server
open Fable.Remoting.Suave

open Microsoft.Data.Sqlite

module Data =

    let connection =
        new SqliteConnection("Data Source=Alchemy.db")

    do connection.Open()

    let private addParm name dbType (cmd : SqliteCommand) =
        cmd.Parameters.Add(name, dbType)
            |> ignore

    let private tryFindCmd =
        let cmd =
            connection.CreateCommand(
                CommandText =
                    "select Generation \
                    from Concept \
                    where Name = $Name;")
        addParm "$Name" SqliteType.Text cmd
        cmd

    let tryFind (concept : Concept) =
        tryFindCmd.Parameters["$Name"].Value <- concept
        let value = tryFindCmd.ExecuteScalar()
        if isNull value then None
        else Some (Convert.ToInt32 value)

    let private upsertCmd =
        let cmd =
            connection.CreateCommand(
                CommandText =
                    "insert into Concept (Name, Generation, First, Second) \
                    values ($Name, $Generation, $First, $Second) \
                    on conflict (Name) do \
                    update set Name = $Name,
                    Generation = $Generation,
                    First = $First,
                    Second = $Second;")
        addParm "$Name" SqliteType.Text cmd
        addParm "$Generation" SqliteType.Integer cmd
        addParm "$First" SqliteType.Text cmd
        addParm "$Second" SqliteType.Text cmd
        cmd

    let upsert
        (concept : Concept)
        (generation : int)
        (first : Concept)
        (second : Concept) =
        upsertCmd.Parameters["$Name"].Value <- concept
        upsertCmd.Parameters["$Generation"].Value <- generation
        upsertCmd.Parameters["$First"].Value <- first
        upsertCmd.Parameters["$Second"].Value <- second
        let nRows = upsertCmd.ExecuteNonQuery()
        assert(nRows = 1)

module Program =

    let private memoize oracle =
        Oracle.combine oracle
            |> uncurry
            |> Prelude.memoize
            |> curry

    let private apply combine first second =
        lock Data.connection (fun () ->
            option {
                let! genFirst = Data.tryFind first
                let! genSecond = Data.tryFind second
                let! concept = combine first second
                let newGen = (max genFirst genSecond) + 1
                let isNew =
                    match Data.tryFind concept with
                        | Some oldGen when oldGen <= newGen ->
                            false
                        | _ ->
                            Data.upsert concept newGen first second
                            true
                return concept, isNew
            })

    try

        let alchemyApi =
            {
                Combine =
                    let combine = memoize (Oracle.create ())
                    fun (first, second) ->
                        async {
                            return apply combine first second
                        }
            }

            // create the web service
        let service : WebPart =
            let logger = Targets.create LogLevel.Info [||]
            (Remoting.createApi()
                |> Remoting.fromValue alchemyApi
                |> Remoting.buildWebPart)
                >=> Filters.logWithLevelStructured
                    LogLevel.Info
                    logger
                    Filters.logFormatStructured

            // start the web server
        let config =
            { defaultConfig with
                bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" 5000 ] }
        startWebServer config service

    with exn -> printfn $"{exn.Message}"
