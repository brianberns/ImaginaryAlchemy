namespace ImaginaryAlchemy

open Feliz

module View =

    /// Renders a single concept "card" containing the name
    /// of the concept, its generation number, and an is-new
    /// indicator.
    let private renderConceptCard concept info =
        Html.div [
            prop.className "concept-card"
            prop.children [

                    // is new?
                if info.IsNew then
                    Html.div [
                        prop.className "discovery"
                        prop.text "★"
                    ]

                    // concept name (e.g. "Water")
                Html.div [
                    prop.className "concept"
                    prop.text (concept : Concept)
                ]

                    // generation number (e.g. 10)
                Html.div [
                    prop.className "generation"
                    prop.text $"{info.Generation}"
                ]
            ]
                // allow card to be dragged
            prop.draggable true
            prop.onDragStart (fun evt ->
                DragData.setConcept concept evt)
        ]

    /// Renders drag/drop properties.
    let private renderDragDrop allow dispatch =
        [
                // start highlighting the target?
            prop.onDragEnter (fun evt ->
                if allow evt |> Option.isSome then
                    evt.preventDefault()
                    (*dispatch "highlight"*))

                // stop highlighting the target?
            prop.onDragLeave (fun evt ->
                if allow evt |> Option.isSome then
                    evt.preventDefault()
                    (*dispatch "no-highlight"*))

                // allow drop?
            prop.onDragOver (fun evt ->
                if allow evt |> Option.isSome then
                    evt.preventDefault())

                // drop has occurred
            prop.onDrop (fun evt ->
                evt.preventDefault()
                match allow evt with
                    | Some (msg : Message) -> dispatch msg
                    | None -> ())
        ]

    /// Renders a spot that might (or might not) contain
    /// a concept card.
    let private renderConceptSpot
        conceptOpt
        (conceptMap : ConceptMap)
        makeMsgOpt
        dispatch =
        Html.div [
            match conceptOpt with

                    // spot is full
                | Some concept ->
                    prop.children [
                        renderConceptCard
                            concept
                            conceptMap[concept]
                    ]

                    // spot is empty
                | None ->
                    prop.className [
                        "empty-concept"
                        if Option.isNone makeMsgOpt then
                            "invisible"
                    ]
                    prop.innerHtml "&nbsp;"

                // allow concepts to be dragged to the spot?
            match makeMsgOpt with
                | Some makeMsg ->
                    yield! renderDragDrop
                        (fun evt ->
                            let concept = DragData.getConcept evt
                            Some (makeMsg concept))
                        dispatch
                | None -> ()
        ]

    /// Renders a workspace containing two input spots (left and right)
    /// and an output spot.
    let private renderWorkspace model dispatch =
        Html.div [
            prop.id "workspace"
            prop.children [

                    // output spot
                Html.div [
                    prop.id "combined-concept"
                    let isNew =
                        model.CombinedOpt
                            |> Option.map snd
                            |> Option.defaultValue false
                    if isNew then
                        prop.className "is-new-global"
                    let combinedOpt =
                        Option.map fst model.CombinedOpt
                    prop.children [
                        renderConceptSpot
                            combinedOpt
                            model.ConceptMap
                            None
                            dispatch
                    ]
                ]
                    // left input spot
                Html.div [
                    prop.id "left-concept"
                    prop.children [
                        renderConceptSpot
                            model.FirstOpt
                            model.ConceptMap
                            (Some SetFirst)
                            dispatch
                    ]
                ]
                    // button that combines the two inputs when clicked
                Html.button [
                    prop.id "combine"
                    prop.text "+"
                    prop.onClick (fun _ ->
                        Combine |> dispatch)
                    prop.disabled
                        (model.IsLoading
                            || model.FirstOpt.IsNone
                            || model.SecondOpt.IsNone)
                ]
                    // right input spot
                Html.div [
                    prop.id "right-concept"
                    prop.children [
                        renderConceptSpot
                            model.SecondOpt
                            model.ConceptMap
                            (Some SetSecond)
                            dispatch
                    ]
                ]
            ]
        ]

    /// Renders a sorted collection cards for known concepts.
    let private renderConceptCards model =

        let sortAlphabetical (concept, _info) =
            concept

        let sortByDiscovered (concept, info) =
            -info.Discovered.Ticks, -info.Generation, concept

        let sortByLastUsed (concept, info) =
            -info.LastUsed.Ticks, -info.Generation, concept

        let sortByGeneration (concept, info) =
            info.Generation, concept

        Html.div [
            prop.id "concept-cards"
            model.ConceptMap
                |> Map.toSeq
                |> match model.SortMode with
                    | Alphabetical -> Seq.sortBy sortAlphabetical
                    | ByDiscovered -> Seq.sortBy sortByDiscovered
                    | ByLastUsed -> Seq.sortBy sortByLastUsed
                    | ByGeneration -> Seq.sortBy sortByGeneration
                |> Seq.map (fun (concept, gen) ->
                    renderConceptCard
                        concept
                        gen)
                |> prop.children
        ]

    /// Renders a sort button.
    let private renderSortButton
        buttonMode
        text
        actualMode
        dispatch =
        Html.button [
            prop.text (text : string)
            if actualMode = buttonMode then
                prop.className "sort-selected"
            prop.onClick (fun _ ->
                SetSortMode buttonMode |> dispatch)
        ]

    /// Renders the footer.
    let private renderFooter sortMode dispatch =
        Html.div [
            prop.id "footer"
            prop.children [
                Html.div [
                    prop.id "footer-sort"
                    prop.children [
                        Html.span [
                            prop.text "Sort:"
                        ]
                        renderSortButton Alphabetical "A-Z"
                            sortMode dispatch
                        renderSortButton ByGeneration "Generation #"
                            sortMode dispatch
                        renderSortButton ByLastUsed "Last used"
                            sortMode dispatch
                        renderSortButton ByDiscovered "When discovered"
                            sortMode dispatch
                    ]
                ]
                Html.div [
                    prop.id "footer-toolbar"
                    prop.children [
                        Html.img [
                            prop.className "settings-button"
                            prop.src "refresh.svg"
                            prop.onClick (fun _ ->
                                if Browser.Dom.window.confirm("You will lose all progress. Are you sure?") then
                                    dispatch Reset)
                        ]
                    ]
                ]
            ]
        ]

    /// Renders a view of the given model.
    let render model dispatch =
        Html.div [
            prop.id "parent"
            if model.IsLoading then
                prop.className "loading"
            prop.children [
                renderWorkspace model dispatch
                renderConceptCards model
                renderFooter model.SortMode dispatch
            ]
        ]        
