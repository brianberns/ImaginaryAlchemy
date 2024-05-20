﻿namespace ImaginaryAlchemy

/// A function that combines concepts.
type Combine = Concept -> Concept -> Option<Concept>

module CombinationCache =

    /// Creates a cache. Not thread-safe.
    let create db (combine : Combine) : Combine =
        fun first second ->
            match Data.getCombination db first second with
                | None ->
                    let conceptOpt = combine first second
                    Data.insertCombination db first second conceptOpt
                    conceptOpt
                | Some conceptOpt -> conceptOpt
