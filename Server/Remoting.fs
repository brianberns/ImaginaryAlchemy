namespace ImaginaryAlchemy

open Fable.Remoting.Server
open Fable.Remoting.Suave

module private Remoting =

    let private memoize oracle =
        Oracle.combine oracle
            |> uncurry
            |> Prelude.memoize
            |> curry

    let private apply data combine first second =
        lock data (fun () ->
            (data.GetGeneration first, data.GetGeneration second)
                ||> Option.lift2 (fun genFirst genSecond ->
                    match combine first second with
                        | Ok concept ->
                            let isNew =
                                let newGen = (max genFirst genSecond) + 1
                                match data.GetGeneration concept with

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
