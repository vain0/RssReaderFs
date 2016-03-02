﻿namespace RssReaderFs

open System
open System.Xml

module Rss =
  let parseRss uri (xml: XmlDocument) =
    let getTextElem xpath =
        Xml.selectSingleNode xpath
        >> Option.map (Xml.innerText)

    let tryBuildItem (xnode: XmlNode) =
        let at = flip getTextElem xnode
        let title = at "title"
        let date  =
            at "pubDate"
            |> Option.bind (DateTime.tryParse)
            |> Option.map (fun time -> time.ToLocalTime())
        in
          match (title, date) with
          | (Some title, Some date) ->
              {
                Title = title
                Desc  = at "description"
                Link  = at "link"
                Date  = date
                Uri   = uri
              } |> Some
          | _ -> None
    in
      xml
      |> Xml.selectNodes "rss/channel/item"
      |> Seq.choose tryBuildItem

  let downloadRssAsync (source: RssSource) =
    async {
      let uri = source.Uri
      let! xml = Net.downloadXmlAsync(uri)
      return (xml |> parseRss uri)
    }

  let downloadFeedAsync lastUpdate (source: RssSource) =
    async {
      let! items = source |> downloadRssAsync
      return
        {
          LastUpdate  = lastUpdate
          Items       = items
          OldItems    = []
          Source      = source
        }
    }

  let updateFeedAsync (feed: RssFeed) =
    async {
      let! newItems = feed.Source |> downloadRssAsync

      // 前回の取得時刻より新しいアイテムのみ
      let newItems =
        newItems
        |> Seq.filter (fun item ->
            item.Date >= feed.LastUpdate
            )

      return
        { feed
          with
            LastUpdate  = DateTime.Now
            OldItems    = feed.Items :: feed.OldItems
            Items       = newItems
        }
    }
