namespace ImaginaryAlchemy

open System

module Data =

    open Microsoft.Data.Sqlite

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
                    update set \
                    Generation = $Generation, \
                    First = $First, \
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

module private Remoting =

    open Fable.Remoting.Server
    open Fable.Remoting.Suave

    let private memoize oracle =
        Oracle.combine oracle
            |> uncurry
            |> Prelude.memoize
            |> curry

    let private apply combine first second =
        lock Data.connection (fun () ->
            (Data.tryFind first, Data.tryFind second)
                ||> Option.lift2 (fun genFirst genSecond ->
                    match combine first second with
                        | Ok concept ->
                            let isNew =
                                let newGen = (max genFirst genSecond) + 1
                                match Data.tryFind concept with

                                        // insert
                                    | None ->
                                        Data.upsert concept newGen first second
                                        true

                                        // update
                                    | Some oldGen when newGen < oldGen ->
                                        Data.upsert concept newGen first second
                                        false

                                        // no change
                                    | Some _ -> false
                            Ok (concept, isNew)
                        | Error str -> Error str)
                |> Option.defaultValue (Error "Invalid"))

    let alchemyApi dir =
        {
            Combine =
                let combine = memoize (Oracle.create dir)
                fun (first, second) ->
                    async {
                        return apply combine first second
                    }
        }

    /// Build API.
    let webPart dir =
        Remoting.createApi()
            |> Remoting.fromValue (alchemyApi dir)
            |> Remoting.buildWebPart

module WebPart =

    open System.IO
    open System.Reflection

    open Suave
    open Suave.Filters
    open Suave.Operators

    /// Web part.
    let app : WebPart =

        let dir =
            Assembly.GetExecutingAssembly().Location
                |> Path.GetDirectoryName
        let staticPath = Path.Combine(dir, "public")

        choose [
            Remoting.webPart dir
            Filters.path "/" >=> Files.browseFile staticPath "index.html"
            GET >=> Files.browse staticPath
        ]
