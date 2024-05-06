namespace ImaginaryAlchemy

open Fable.Remoting.Client
open Elmish

module Alchemy =

    let api =
        Remoting.createApi()
            |> Remoting.buildProxy<IAlchemyApi>

type Model =
    {
        ConceptMap : ConceptMap
        FirstOpt : Option<Concept>
        SecondOpt : Option<Concept>
        CombinedOpt : Option<Concept>
        IsLoading : bool
    }

type Message =
    | SetFirst of Concept
    | SetSecond of Concept
    | Combine
    | Upsert of Concept * (*generation*) int * CombinationResultType
    | Fail

module Model =

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

    let private onCombineSuccess model first second gen = function
        | Ok (concept, resultType) ->
            let gen' =
                model.ConceptMap
                    |> Map.tryFind concept
                    |> Option.map (fun info ->
                        min gen info.Generation)
                    |> Option.defaultValue gen
            let resultTypeStr =
                match resultType with
                    | NewConcept ->" [new discovery!!]"
                    | NewGeneration -> " [new generation!]"
                    | Existing -> ""
            Browser.Dom.console.log(
                $"{first} + {second} = {concept}{resultTypeStr}")
            Upsert (concept, gen', resultType)
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
                        (onCombineSuccess model' first second gen)
                        (onCombineError)
            } |> Option.defaultValue Cmd.none
        model', cmd

    let private upsert concept gen resultType model =
        let model' =
            let info =
                ConceptInfo.create gen resultType
            { model with
                ConceptMap =
                    Map.add concept info model.ConceptMap
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
            | Upsert (concept, gen, resultType) ->
                upsert concept gen resultType model
            | Fail ->
                fail model
