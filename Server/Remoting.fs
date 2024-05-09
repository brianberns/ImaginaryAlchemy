namespace ImaginaryAlchemy

open Fable.Remoting.Server
open Fable.Remoting.Suave

module private Remoting =

    /// Memoizes the given oracle to prevent redundant queries.
    let private memoize oracle =
        Oracle.combine oracle
            |> uncurry
            |> Prelude.memoize
            |> curry

    /// Combines the given concepts using the given function.
    let private apply db combine first second =

            // lock the database in case we have to change it
        lock db (fun () ->
            option {

                    // get generation numbers of parent concepts
                let! genFirst = Data.getGeneration db first
                let! genSecond = Data.getGeneration db second

                    // combine concepts
                let! concept = combine first second

                    // is this a new concept?
                let isNew =

                        // compute the new generation number
                    let newGen = (max genFirst genSecond) + 1

                        // is there an existing generation number for this concept?
                    match Data.getGeneration db concept with

                        | None ->       // no, it's new!!
                            Data.upsert db concept newGen first second
                            true

                        | Some oldGen   // yes, but this is a shorter path!
                            when newGen < oldGen ->
                            Data.upsert db concept newGen first second
                            false

                        | Some _ ->     // yes, and this path isn't any better
                            false

                let isNewStr =
                    if isNew then " [new!]"
                    else ""
                printfn $"{first} + {second} = {concept}{isNewStr}"
                return concept, isNew
            })

    /// Server API.
    let private alchemyApi dir =
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
