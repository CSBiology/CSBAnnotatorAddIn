module AddBuildingBlockView

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages
open Shared
open CustomComponents

let isValidBuildingBlock (block : AnnotationBuildingBlock) =
    match block.Type with
    | Parameter | Characteristics | Factor ->
        block.Name.Length > 0
    | Sample | Data ->
        true
    | _ -> false

let createUnitTermSuggestions (model:Model) (dispatch: Msg -> unit) =
    if model.AddBuildingBlockState.UnitTermSuggestions.Length > 0 then
        model.AddBuildingBlockState.UnitTermSuggestions
        |> fun s -> s |> Array.take (if s.Length < 5 then s.Length else 5)
        |> Array.map (fun sugg ->
            tr [OnClick (fun _ -> sugg |> UnitTermSuggestionUsed |> AddBuildingBlock |> dispatch)
                colorControl model.SiteStyleState.ColorMode
                Class "suggestion"
            ] [
                td [Class (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline);Tooltip.dataTooltip sugg.Definition] [
                    Fa.i [Fa.Solid.InfoCircle] []
                ]
                td [] [
                    b [] [str sugg.Name]
                ]
                td [Style [Color "red"]] [if sugg.IsObsolete then str "obsolete"]
                td [Style [FontWeight "light"]] [small [] [str sugg.Accession]]
            ])
        |> List.ofArray
    else
        [
            tr [] [
                td [] [str "No terms found matching your input."]
            ]
        ]

let createBuildingBlockDropdownItem (model:Model) (dispatch:Msg -> unit) (block: AnnotationBuildingBlockType )  =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            OnClick (fun _ -> AnnotationBuildingBlock.init block |> NewBuildingBlockSelected |> AddBuildingBlock |> dispatch)
            colorControl model.SiteStyleState.ColorMode
        ]

    ][
        Text.span [
            CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
            Props [
                Tooltip.dataTooltip (block |> AnnotationBuildingBlockType.toShortExplanation)
                Style [PaddingRight "10px"]
            ]
        ] [
            Fa.i [Fa.Solid.InfoCircle] []
        ]
        
        Text.span [] [block |> AnnotationBuildingBlockType.toString |> str]
    ]

let addBuildingBlockFooterComponent (model:Model) (dispatch:Msg -> unit) =
    Content.content [] [
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ 
            str (sprintf "More about %s:" (model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toString))
        ]
        Text.p [] [
            model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toLongExplanation |> str
        ]
    ]

let addBuildingBlockComponent (model:Model) (dispatch:Msg -> unit) =
    form [
        OnSubmit (fun e -> e.preventDefault())
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Annotation building block selection"]

        Field.div [] [
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Building block"]
            Help.help [] [str "Select the type of annotation building block (column) to add to the annotation table"]
            Field.div [Field.HasAddons] [
                Control.div [] [
                    Dropdown.dropdown [Dropdown.IsActive model.AddBuildingBlockState.ShowBuildingBlockSelection] [
                        Dropdown.trigger [] [
                            Button.button [Button.OnClick (fun _ -> ToggleSelectionDropdown |> AddBuildingBlock |> dispatch)] [
                                span [] [model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toString |> str]
                                Fa.i [Fa.Solid.AngleDown] []
                            ]
                        ]
                        Dropdown.menu [Props[colorControl model.SiteStyleState.ColorMode]] [
                            Dropdown.content [] ([
                                Parameter       
                                Factor          
                                Characteristics 
                                Sample          
                                Data            
                                Source          
                            ]  |> List.map (createBuildingBlockDropdownItem model dispatch))
                        ]
                    ]
                ]
                Control.div [Control.IsExpanded] [
                    match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
                    | Parameter | Characteristics | Factor ->
                        Input.input [
                            Input.Placeholder (sprintf "Enter %s Name" (model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toString))
                            Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                            Input.OnChange (fun ev -> ev.Value |> BuildingBlockNameChange |> AddBuildingBlock |> dispatch)
                        ]
                    | _ -> ()
                ]
            ]
            match model.AddBuildingBlockState.CurrentBuildingBlock.Type with
            | Parameter | Characteristics | Factor ->
                Field.div [Field.HasAddons] [
                    Control.div [] [
                        Button.button [ Button.OnClick (fun _ -> BuildingBlockHasUnitSwitch |> AddBuildingBlock |> dispatch)] [ 
                            Checkbox.checkbox [] [
                                Checkbox.input [
                                    Props [
                                        Checked model.AddBuildingBlockState.BuildingBlockHasUnit
                                        
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Control.p [] [
                        Button.button [Button.IsStatic true] [
                            str (sprintf "This %s has a unit:" (model.AddBuildingBlockState.CurrentBuildingBlock.Type |> AnnotationBuildingBlockType.toString ))
                        ]
                    ]
                    Control.div [Control.IsExpanded] [
                        Input.input [
                                        Input.Disabled (not model.AddBuildingBlockState.BuildingBlockHasUnit)
                                        Input.Placeholder "Start typing to start search"
                                        Input.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]
                                        Input.OnChange (fun e ->  e.Value |> SearchUnitTermTextChange |> AddBuildingBlock |> dispatch)
                                        Input.Value(
                                            if model.AddBuildingBlockState.BuildingBlockHasUnit then 
                                                model.AddBuildingBlockState.UnitTermSearchText
                                            else
                                                ""
                                        )
                                    ]
                        AutocompleteDropdown.autocompleteDropdownComponent
                            model
                            dispatch
                            model.AddBuildingBlockState.ShowUnitTermSuggestions
                            model.AddBuildingBlockState.HasUnitTermSuggestionsLoading
                            (createUnitTermSuggestions model dispatch)
                    ]
                ]
            | _ -> ()
        ]

        // Fill selection confirmation
        Field.div [] [
            Control.div [] [
                Button.button   [   let isValid = model.AddBuildingBlockState.CurrentBuildingBlock |> isValidBuildingBlock
                                    if isValid then
                                        Button.CustomClass "is-success"
                                        Button.IsActive true
                                    else
                                        Button.CustomClass "is-danger"
                                        Button.Props [Disabled true]
                                    Button.IsFullWidth
                                    //TODO: add fill support via Excel interop here
                                    Button.OnClick (
                                        let format =
                                            match model.AddBuildingBlockState.UnitTerm with
                                            | Some unit ->
                                                sprintf "0.00 \"%s\"" unit.Name
                                            | _ -> "0.00"
                                        let colName = model.AddBuildingBlockState.CurrentBuildingBlock |> AnnotationBuildingBlock.toAnnotationTableHeader
                                        fun _ -> (colName,format) |> AddColumn |> ExcelInterop |> dispatch
                                    )

                                ] [
                    str "Insert this annotation building block"
                ]
            ]
        ]
    ]