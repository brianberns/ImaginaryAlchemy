namespace ImaginaryAlchemy

module OracleCache =

    /// Combines the given concepts, if possible.
    let private combine db (oracle : Oracle) first second =
        lock db (fun () ->
            if oracle.IsValid first second then
                match Data.getCombination db first second with
                    | None ->
                        let conceptOpt =
                            oracle.Combine first second
                        Data.insertCombination
                            db first second conceptOpt
                        conceptOpt
                    | Some conceptOpt -> conceptOpt
            else None)

    /// Creates a cache for the given oracle.
    let create db (oracle : Oracle) =
        {
            oracle with
                Combine = combine db oracle
        }
