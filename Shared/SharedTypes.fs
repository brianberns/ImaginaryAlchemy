namespace ImaginaryAlchemy

type Concept = string

type IAlchemyApi =
    {
        Combine : (Concept * Concept) -> Async<Option<Concept * int>>
    }
