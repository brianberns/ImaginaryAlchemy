namespace ImaginaryAlchemy

open Feliz

module View =

    let private renderConceptCard concept info =
        Html.div [
            prop.className "concept-card"
            prop.children [
                if info.IsNew then
                    Html.div [
                        prop.className "discovery"
                        prop.text "★"
                    ]
                Html.div [
                    prop.className "concept"
                    prop.text (concept : Concept)
                ]
                Html.div [
                    prop.className "generation"
                    prop.text $"{info.Generation}"
                ]
            ]
            prop.draggable true
            prop.onDragStart (fun evt ->
                // Audio.enable ()
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
                    | Some msg -> dispatch msg
                    | None -> ())
        ]

    let private renderConceptSpot
        conceptOpt
        (conceptMap : ConceptMap)
        makeMsgOpt
        dispatch =
        Html.div [
            match conceptOpt with
                | Some concept ->
                    prop.children [
                        renderConceptCard
                            concept
                            conceptMap[concept]
                    ]
                | None ->
                    prop.className [
                        "empty-concept"
                        if Option.isNone makeMsgOpt then
                            "invisible"
                    ]
                    prop.innerHtml "&nbsp;"
            match makeMsgOpt with
                | Some makeMsg ->
                    yield! renderDragDrop
                        (fun evt ->
                            let concept = DragData.getConcept evt
                            Some (makeMsg concept))
                        dispatch
                | None -> ()
        ]

    let private renderWorkspace model dispatch =
        Html.div [
            prop.id "workspace"
            prop.children [

                Html.div [
                    prop.id "combined-concept"
                    prop.children [
                        renderConceptSpot
                            model.CombinedOpt
                            model.ConceptMap
                            None
                            dispatch
                    ]
                ]

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

    let private renderConceptCards model =

        let sortAlphabetical (concept, info) =
            concept

        let sortByDiscovered (concept, info) =
            -info.Discovered.Ticks, -info.Generation, concept

        let sortByLastUsed (concept, info) =
            -info.LastUsed.Ticks, -info.Generation, concept

        Html.div [
            prop.id "concept-cards"
            model.ConceptMap
                |> Map.toSeq
                |> match model.SortMode with
                    | Alphabetical -> Seq.sortBy sortAlphabetical
                    | ByDiscovered -> Seq.sortBy sortByDiscovered
                    | ByLastUsed -> Seq.sortBy sortByLastUsed
                |> Seq.map (fun (concept, gen) ->
                    renderConceptCard
                        concept
                        gen)
                |> prop.children
        ]

    let private renderFooter sortMode dispatch =
        Html.div [
            prop.id "footer"
            prop.children [
                Html.span [
                    prop.text "Sort:"
                ]
                Html.button [
                    prop.text "A-Z"
                    if sortMode = Alphabetical then
                        prop.className "sort-selected"
                    prop.onClick (fun _ ->
                        SetSortMode Alphabetical |> dispatch)
                ]
                Html.button [
                    prop.text "When discovered"
                    if sortMode = ByDiscovered then
                        prop.className "sort-selected"
                    prop.onClick (fun _ ->
                        SetSortMode ByDiscovered |> dispatch)
                ]
                Html.button [
                    prop.text "Last used"
                    if sortMode = ByLastUsed then
                        prop.className "sort-selected"
                    prop.onClick (fun _ ->
                        SetSortMode ByLastUsed |> dispatch)
                ]
            ]
        ]

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
