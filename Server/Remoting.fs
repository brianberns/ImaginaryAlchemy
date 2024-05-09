namespace ImaginaryAlchemy

open Fable.Remoting.Server
open Fable.Remoting.Suave

module private Remoting =

    let private memoize oracle =
        Oracle.combine oracle
            |> uncurry
            |> Prelude.memoize
            |> curry

    let private apply db combine first second =
        lock db (fun () ->
            (Data.getGeneration db first,
            Data.getGeneration db second)
                ||> Option.lift2 (fun genFirst genSecond ->
                    match combine first second with
                        | Ok concept ->
                            let isNew =
                                let newGen = (max genFirst genSecond) + 1
                                match Data.getGeneration db concept with

                                        // insert
                                    | None ->
                                        Data.upsert db concept newGen first second
                                        true

                                        // update
                                    | Some oldGen when newGen < oldGen ->
                                        Data.upsert db concept newGen first second
                                        false

                                        // no change
                                    | Some _ -> false
                            Ok (concept, isNew)
                        | Error str -> Error str)
                |> Option.defaultValue (Error "Invalid"))

    let alchemyApi dir =
        let db = Data.connect dir
        {
            Combine =
                let combine = memoize (Oracle.create dir)
                fun (first, second) ->
                    async {
                        return apply db combine first second
                    }
        }

    /// Build API.
    let webPart dir =
        Remoting.createApi()
            |> Remoting.fromValue (alchemyApi dir)
            |> Remoting.buildWebPart
