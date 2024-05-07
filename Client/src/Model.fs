namespace ImaginaryAlchemy

open Fable.Remoting.Client
open Elmish

module Alchemy =

    /// Server API.
    let api =
        Remoting.createApi()
            |> Remoting.buildProxy<IAlchemyApi>

/// Immutable model.
type Model =
    {
        /// All concepts seen by this client so far.
        ConceptMap : ConceptMap

        /// Concept ready to be combined?
        FirstOpt : Option<Concept>

        /// Concept ready to be combined?
        SecondOpt : Option<Concept>

        /// Result of combining the two concepts?
        CombinedOpt : Option<Concept>

        /// Waiting for server?
        IsLoading : bool
    }

/// Messages that operate on the model.
type Message =

    /// Set concept to combine.
    | SetFirst of Concept

    /// Set concept to combine
    | SetSecond of Concept

    /// Combine the two concepts.
    | Combine

    /// Save result of combining the two concepts.
    | Upsert of Concept * (*generation*) int * (*isnew*) bool

    /// Concepts could not be combined.
    | Fail

module Model =

    /// Initial model.
    let init () =
        let model =
            {
                ConceptMap = Settings.Current.ConceptMap
                FirstOpt = None
                SecondOpt = None
                CombinedOpt = None
                IsLoading = false
            }
        model, Cmd.none

    let private setFirst concept model =
        { model with
            FirstOpt = Some concept
            CombinedOpt = None },
        Cmd.none

    let private setSecond concept model =
        { model with
            SecondOpt = Some concept
            CombinedOpt = None },
        Cmd.none

    let private combineAsync first second () =
        Alchemy.api.Combine(first, second)

    let private onCombineSuccess first second gen = function
        | Ok (concept, isNew) ->
            let isNewStr =
                if isNew then " [new!]"
                else ""
            Browser.Dom.console.log(
                $"{first} + {second} = {concept}{isNew}")
            Upsert (concept, gen, isNew)
        | Error msg ->
            Browser.Dom.console.log(
                $"{first} + {second} = {msg} [failed]")
            Fail

    let private onCombineError (exn : exn) =
        Browser.Dom.window.alert(exn.Message)
        Fail

    let private combine model =
        let model' =
            { model with IsLoading = true }
        let cmd =
            option {
                let! first = model'.FirstOpt
                let! second = model'.SecondOpt
                let gen =
                    let genFirst =
                        model'.ConceptMap[first].Generation
                    let genSecond =
                        model'.ConceptMap[second].Generation
                    (max genFirst genSecond) + 1
                return
                    Cmd.OfAsync.either
                        (combineAsync first second)
                        ()
                        (onCombineSuccess first second gen)
                        (onCombineError)
            } |> Option.defaultValue Cmd.none
        model', cmd

    let private upsert concept gen isNew model =
        let model' =
            let conceptMap =
                match Map.tryFind concept model.ConceptMap with
                    | Some info when info.Generation <= gen ->
                        model.ConceptMap
                    | _ ->
                        let info =
                            ConceptInfo.discover gen isNew
                        Map.add concept info model.ConceptMap
            { model with
                ConceptMap = conceptMap
                CombinedOpt = Some concept
                IsLoading = false }
        Settings.save {
            Settings.Current with
                ConceptMap = model'.ConceptMap
        }
        model', Cmd.none

    let private fail model =
        let model' =
            { model with IsLoading = false }
        model', Cmd.none

    let update msg model =
        match msg with
            | SetFirst concept ->
                setFirst concept model
            | SetSecond concept ->
                setSecond concept model
            | Combine ->
                combine model
            | Upsert (concept, gen, isNew) ->
                upsert concept gen isNew model
            | Fail ->
                fail model
