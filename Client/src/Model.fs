namespace ImaginaryAlchemy

open System
open Fable.Remoting.Client
open Elmish

module Alchemy =

    /// Prefix routes with /Alchemy.
    let routeBuilder typeName methodName = 
        sprintf "/Alchemy/%s/%s" typeName methodName

    /// Server API.
    let api =
        Remoting.createApi()
            |> Remoting.withRouteBuilder routeBuilder
            |> Remoting.buildProxy<IAlchemyApi>

/// Immutable model.
type Model =
    {
        /// All concepts seen by this client so far.
        ConceptMap : ConceptMap

        /// How to sort concepts.
        SortMode : SortMode

        /// Concept ready to be combined?
        FirstOpt : Option<Concept>

        /// Concept ready to be combined?
        SecondOpt : Option<Concept>

        /// Result of combining the two concepts?
        CombinedOpt : Option<Concept * (*isNew*) bool>

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

    /// Set sort mode.
    | SetSortMode of SortMode

    /// Concepts could not be combined.
    | Fail

module Model =

    /// Initializes a model from user's current settings.
    let init () =
        let model =
            let settings = Settings.Current
            {
                ConceptMap = settings.ConceptMap
                SortMode = settings.SortMode
                FirstOpt = None
                SecondOpt = None
                CombinedOpt = None
                IsLoading = false
            }
        model, Cmd.none

    /// Sets a concept to combine.
    let private setFirst concept model =
        { model with
            FirstOpt = Some concept
            CombinedOpt = None },
        Cmd.none

    /// Sets a concept to combine.
    let private setSecond concept model =
        { model with
            SecondOpt = Some concept
            CombinedOpt = None },
        Cmd.none

    /// Asks server to combine the two given concepts asynchronously.
    let private combineAsync first second () =
        Alchemy.api.Combine(first, second)

    /// On combination response from server.
    let private onCombineResponse first second gen = function

            // concepts combined
        | Ok (concept, isNew) ->
            let isNewStr =
                if isNew then " [new!]"
                else ""
            Browser.Dom.console.log(
                $"{first} + {second} = {concept} {isNewStr}")
            Upsert (concept, gen, isNew)

            // concpets wouldn't combine
        | Error msg ->
            Browser.Dom.console.log(
                $"{first} + {second} = {msg} [failed]")
            Fail

    /// On server error (e.g. server not running).
    let private onCombineError (exn : exn) =
        Browser.Dom.window.alert(exn.Message)
        Fail

    /// Combines two concepts.
    let private combine model =
        option {
                // concepts to combine
            let! first = model.FirstOpt
            let! second = model.SecondOpt
            let! firstInfo = Map.tryFind first model.ConceptMap
            let! secondInfo = Map.tryFind second model.ConceptMap

                // update timestamp for each input
            let model' =
                let now = DateTime.Now
                let conceptMap =
                    model.ConceptMap
                        |> Map.add first
                            { firstInfo with LastUsed = now }
                        |> Map.add second
                            { secondInfo with LastUsed = now }
                {
                    model with
                        IsLoading = true   // waiting for server
                        ConceptMap = conceptMap
                }

                // attempt to combine concepts
            let cmd =
                let gen =
                    (max
                        firstInfo.Generation
                        secondInfo.Generation) + 1
                Cmd.OfAsync.either
                    (combineAsync first second)
                    ()
                    (onCombineResponse first second gen)
                    (onCombineError)

            return model', cmd
        } |> Option.defaultValue (model, Cmd.none)

    /// Saves result of combining two concepts.
    let private upsert concept gen isNew model =

            // update this client's knowledge about the resulting concept
        let model' =
            let conceptMap =
                match Map.tryFind concept model.ConceptMap with

                        // nothing to change
                    | Some info when info.Generation <= gen ->
                        model.ConceptMap

                        // update with new info:
                        // * either an earlier generation of a concept
                        //   already known to this client,
                        // * or a concept not seen before by this client)
                    | _ ->
                        let info =
                            ConceptInfo.discover gen isNew
                        Map.add concept info model.ConceptMap
            { model with
                ConceptMap = conceptMap
                CombinedOpt = Some (concept, isNew)
                IsLoading = false }

            // persist knowledge on this client
        Settings.save {
            Settings.Current with
                ConceptMap = model'.ConceptMap
        }

        model', Cmd.none

    /// Sets the sort mode.
    let private setSortMode mode (model : Model) =
        Settings.save {
            Settings.Current with
                SortMode = mode
        }
        { model with SortMode = mode }, Cmd.none

    /// Concepts failed to combine.
    let private fail model =
        let model' =
            { model with IsLoading = false }
        model', Cmd.none

    /// Applies the given message to the given model.
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
            | SetSortMode mode ->
                setSortMode mode model
            | Fail ->
                fail model
