namespace ImaginaryAlchemy

module private Remoting =

    open Fable.Remoting.Server
    open Fable.Remoting.Suave

    let private memoize oracle =
        Oracle.combine oracle
            |> uncurry
            |> Prelude.memoize
            |> curry

    let private apply data combine first second =
        lock data (fun () ->
            (data.TryFind first, data.TryFind second)
                ||> Option.lift2 (fun genFirst genSecond ->
                    match combine first second with
                        | Ok concept ->
                            let isNew =
                                let newGen = (max genFirst genSecond) + 1
                                match data.TryFind concept with

                                        // insert
                                    | None ->
                                        data.Upsert concept newGen first second
                                        true

                                        // update
                                    | Some oldGen when newGen < oldGen ->
                                        data.Upsert concept newGen first second
                                        false

                                        // no change
                                    | Some _ -> false
                            Ok (concept, isNew)
                        | Error str -> Error str)
                |> Option.defaultValue (Error "Invalid"))

    let alchemyApi dir =
        let data = Data.connect dir
        {
            Combine =
                let combine = memoize (Oracle.create dir)
                fun (first, second) ->
                    async {
                        return apply data combine first second
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
