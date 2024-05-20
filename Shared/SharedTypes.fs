namespace ImaginaryAlchemy

/// Option computation expression builder.
type OptionBuilder() =
    member _.Bind(opt, f) = Option.bind f opt
    member _.Return(x) = Some x
    member _.ReturnFrom(opt : Option<_>) = opt
    member _.Zero() = None

[<AutoOpen>]
module OptionBuilder =

    /// Option computation expression builder.
    let option = OptionBuilder()

/// Something that can be combined with another thing to
/// produce a new thing.
type Concept = string

/// Client/server interface.
type IAlchemyApi =
    {
        /// Attempts to combine the given concepts asynchronously.
        Combine :
            (Concept * Concept) ->
                Async<
                    Option<
                        Concept * (*isNew*) bool>>
    }
