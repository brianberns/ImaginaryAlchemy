namespace ImaginaryAlchemy

open Fable.Remoting.Server
open Fable.Remoting.Suave

module private Remoting =

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
                            Data.upsertConcept db concept newGen first second
                            true

                        | Some oldGen   // yes, but this is a shorter path!
                            when newGen < oldGen ->
                            Data.upsertConcept db concept newGen first second
                            false

                        | Some _ ->     // yes, and this path isn't any better
                            false

                let isNewStr =
                    if isNew then " [new!]"
                    else ""
                printfn $"{first} + {second} = {concept}{isNewStr}"
                return concept, isNew
            })

    /// Creates a function that can combine concepts asynchronously.
    let private createAsyncCombine dir db =

            // create and memoize an oracle
        let combine =
            Oracle.create dir
                |> Oracle.combine
                |> uncurry
                |> Prelude.memoize
                |> curry

        fun (first, second) ->
            async {
                    // normalize input order
                let first, second =
                    min first second,
                    max first second

                    // combine inputs synchronously
                return apply db combine first second
            }

    /// Server API.
    let private alchemyApi dir =
        let db = Data.connect dir
        {
            Combine = createAsyncCombine dir db
        }

    /// Build API.
    let webPart dir =
        Remoting.createApi()
            |> Remoting.fromValue (alchemyApi dir)
            |> Remoting.buildWebPart
