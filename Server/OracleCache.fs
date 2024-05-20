namespace ImaginaryAlchemy

module OracleCache =

    /// Combines the given concepts, if possible.
    let combine db oracle first second =
        if Oracle.isValid oracle first second then
            lock db (fun () ->
                match Data.getCombination db first second with
                    | None ->
                        let conceptOpt =
                            Oracle.combine oracle first second
                        Data.insertCombination db first second conceptOpt
                        conceptOpt
                    | Some conceptOpt -> conceptOpt)
        else None
