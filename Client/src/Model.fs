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

    /// Initializes a model from user's current settings.
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
                $"{first} + {second} = {concept} ({gen}){isNewStr}")
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

            // wait for server
        let model' =
            { model with IsLoading = true }

        let cmd =
            option {
                    // concepts to combine
                let! first = model'.FirstOpt
                let! second = model'.SecondOpt

                    // determine what the generation of a successful
                    // combination will be
                let gen =
                    let genFirst =
                        model'.ConceptMap[first].Generation
                    let genSecond =
                        model'.ConceptMap[second].Generation
                    (max genFirst genSecond) + 1

                    // attempt to combine concepts
                return
                    Cmd.OfAsync.either
                        (combineAsync first second)
                        ()
                        (onCombineResponse first second gen)
                        (onCombineError)
            } |> Option.defaultValue Cmd.none

        model', cmd

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
                CombinedOpt = Some concept
                IsLoading = false }

            // persist knowledge on this client
        Settings.save {
            Settings.Current with
                ConceptMap = model'.ConceptMap
        }

        model', Cmd.none

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
            | Fail ->
                fail model
