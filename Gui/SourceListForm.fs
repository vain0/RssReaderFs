﻿namespace RssReaderFs.Gui

open System
open System.Drawing
open System.Windows.Forms
open RssReaderFs

type SourceAddForm (onRegister: RssSource -> unit) as this =
  inherit Form
    ( Text    = "Add Source - RssReaderFs.Gui"
    , Size    = Size(360, 240)
    )

  let nameLabel =
    new Label
      ( Location    = Point(5, 5)
      , Size        = Size(60, 25)
      , Font        = yuGothic10
      , Text        = "Name:"
      )

  let uriLabel =
    new Label
      ( Location    = Point(5, 5 + nameLabel.Size.Height + 5)
      , Size        = nameLabel.Size
      , Font        = yuGothic10
      , Text        = "Uri:"
      )

  let nameBox =
    new TextBox
      ( Location    = Point(5 + nameLabel.Size.Width + 5, 5)
      , Size        = Size(260, 20)
      , Font        = yuGothic10
      )

  let uriBox =
    new TextBox
      ( Location    = Point(nameBox.Location.X, uriLabel.Location.Y)
      , Size        = nameBox.Size
      , Font        = yuGothic10
      )

  let okButton =
    new Button
      ( Location    =
          Point
            ( this.ClientSize.Width - 70
            , this.ClientSize.Height - 35
            )
      , Size        = Size(60, 25)
      , Font        = yuGothic10
      , Text        = "OK"
      )

  let controls =
    [|
      nameLabel     :> Control
      uriLabel      :> Control
      nameBox       :> Control
      uriBox        :> Control
      okButton      :> Control
    |]

  do
    okButton.Click.Add (fun e ->
      let item =
        {
          Name        = nameBox.Text
          Uri         = Uri(uriBox.Text)
          LastUpdate  = DateTime.Now
        }
      do
        onRegister item
        this.Close()
      )

    this.Shown.Add (fun e ->
      nameBox.Text    <- ""
      uriBox.Text     <- ""
      )

    base.Controls.AddRange(controls)

type SourceListForm (rc: RssClient) as this =
  inherit Form
    ( Text    = "Sources - RssReaderFs.Gui"
    , Size    = Size(480, 360)
    )

  let lvItemFromRssSource (src: RssSource) =
    ListViewItem([| src.Name; string src.Uri |])

  let listView =
    new ListView
      ( Location    = Point(5, 5)
      , Size        = Size(this.ClientSize.Width - 10, this.ClientSize.Height - 40)
      , Font        = yuGothic10
      , View        = View.Details
      )
    |> tap (fun listView ->
        // Add columns

        let columns =
          [|
            "Name"
            "Uri"
          |]
          |> Array.map (fun text -> new ColumnHeader(Text = text))
        do listView.Columns.AddRange(columns)

        // Add initial rows

        rc.Reader |> RssReader.sources
        |> Array.map lvItemFromRssSource
        |> (fun lvItems ->
            listView.Items.AddRange(lvItems)
            )
        )

  let addButton =
    new Button
      ( Location    = Point(5, this.ClientSize.Height - 5 - 25)
      , Size        = Size(80, 25)
      , Font        = yuGothic10
      , Text        = "Add"
      )

  let removeButton =
    new Button
      ( Location    = Point(5 + addButton.Size.Width + 5, addButton.Location.Y)
      , Size        = addButton.Size
      , Font        = yuGothic10
      , Text        = "Remove"
      )

  let controls =
    [|
      listView      :> Control
      addButton     :> Control
      removeButton  :> Control
    |]

  let addForm =
    new SourceAddForm
      (fun src ->
        if src.Name <> "" then
          rc.Add(src)

        listView.Items.Add(lvItemFromRssSource src) |> ignore
        )

  do
    addButton.Click.Add (fun e ->
      addForm.Show()
      )

    removeButton.Click.Add (fun e ->
      let selectedItems = listView.SelectedItems
      for i in 0..(selectedItems.Count - 1) do
        let lvItem = selectedItems.Item(i)
        let uri = Uri(lvItem.SubItems.Item(1).Text)
        do rc.Remove(uri)
      )

    base.Controls.AddRange(controls)
