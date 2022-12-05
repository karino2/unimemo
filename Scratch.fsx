
//
// Twiter関連
//
#r "nuget: FSharp.Data"
#load "Common.fs"
#load "Twitter.fs"

open System
open Twitter
// fsharplint:disable Hints


// ダウンロードした一回目だけ実行
setupTempJson ()
let tweets = TweetParser.Load(__SOURCE_DIRECTORY__ + "/temp_tweet.json")

// let tweets = TweetParser.GetSamples()

let tdict = tweets2dict tweets

convall tdict

// ここまで。

// 以下はテスト
// 一つだけコンバートするテスト
convone tdict (DateTime(2014, 3, 13))
convone tdict (DateTime(2014, 3, 8))


//
// いつなに、のSQLiteからのコンバート。日付がlongなのを直すくらい
//

let itsuNaniDir = "/Users/arinokazuma/Google ドライブ/notes/original/itsunani"
open System.IO


let gn3csv = Path.Combine(itsuNaniDir, "itsunani_GN3.csv")

let lines = File.ReadLines(gn3csv) |> Seq.map (fun line-> line.Split(",")) |> Seq.toList

open System

let msstr2dt str =
    DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(float (Int64.Parse(str)))


let fmtline (dt:DateTime) body =
    let dtstr = dt.ToString("yyyy-MM-dd HH:mm")
    $"- {dtstr} {body}"

let arr2line (arr: string array) =
    let body = arr.[1]
    let dt = msstr2dt arr.[2]
    fmtline dt body


let md1 = lines.[1..] |> List.map arr2line 

let tsv = Path.Combine(itsuNaniDir, "itsunani_rakutenmini_20210418.tsv")
let lines2 = File.ReadLines(tsv) |> Seq.map (fun line-> line.Split("\t")) |> Seq.toList


let arr2line2 (arr: string array) =
    let body = arr.[2]
    let dt = msstr2dt arr.[3]
    fmtline dt body

let md2 = lines2 |> List.map arr2line2

let mdall = List.append md1 md2

// なんかファイル名の日本語が化けたので適当に吐いて手動でリネームした。
let dest = "/Users/arinokazuma/Google ドライブ/DriveText/TeFWiki/itsunani_old.md"

File.WriteAllLines(dest, mdall)

// podcastのRSSからmdを生成

#r "nuget: FSharp.Data"
#r "System.Xml.Linq.dll"
#load "Common.fs"
open System.IO

let podcast_rss_path = $"{Common.unimemoRoot}/original/anchor_rss/rss.txt"

type AnchorRss = FSharp.Data.XmlProvider<"""<?xml version="1.0" encoding="UTF-8"?><rss xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:content="http://purl.org/rss/1.0/modules/content/" xmlns:atom="http://www.w3.org/2005/Atom" version="2.0" xmlns:itunes="http://www.itunes.com/dtds/podcast-1.0.dtd" xmlns:anchor="https://anchor.fm/xmlns">	<channel>
		<title><![CDATA[MyPodcast]]></title>
		<description><![CDATA[mydescription]]></description>
		<link>https://anchor.fm/karino2</link>
		<image>
			<url>https://d3t3ozftmdmh3i.cloudfront.net/production/podcast_uploaded/998960/998960-1535212397504-93ed2911e3e38.jpg</url>
			<title>mypodcast</title>
			<link>https://anchor.fm/karino2</link>
		</image>
		<generator>Anchor Podcasts</generator>
		<lastBuildDate>Wed, 30 Nov 2022 11:02:47 GMT</lastBuildDate>
		<atom:link href="https://anchor.fm/s/68ce140/podcast/rss" rel="self" type="application/rss+xml"/>
		<author><![CDATA[Kazuma Arino]]></author>
		<copyright><![CDATA[Kazuma Arino]]></copyright>
		<language><![CDATA[ja-JP]]></language>
		<atom:link rel="hub" href="https://pubsubhubbub.appspot.com/"/>
		<itunes:author>Kazuma Arino</itunes:author>
		<itunes:summary>mysummary</itunes:summary>
		<itunes:type>episodic</itunes:type>
		<itunes:owner>
			<itunes:name>Kazuma Arino</itunes:name>
			<itunes:email>podcasts7+68ce140@anchor.fm</itunes:email>
		</itunes:owner>
		<itunes:explicit>No</itunes:explicit>
		<itunes:category text="Technology"/>
		<itunes:image href="https://d3t3ozftmdmh3i.cloudfront.net/production/podcast_uploaded/998960/998960-1535212397504-93ed2911e3e38.jpg"/>
		<item>
			<title><![CDATA[220 title]]></title>
			<description><![CDATA[220 desc]]></description>
			<link>https://anchor.fm/karino2/episodes/220-e1rhbj6</link>
			<guid isPermaLink="false">0caee028-2b59-445b-a6a9-02afb998409d</guid>
			<dc:creator><![CDATA[Kazuma Arino]]></dc:creator>
			<pubDate>Wed, 30 Nov 2022 11:00:06 GMT</pubDate>
			<enclosure url="https://anchor.fm/s/68ce140/podcast/play/61434918/https%3A%2F%2Fd3ctxlq1ktw2nl.cloudfront.net%2Fproduction%2F2022-10-30%2F300132704-44100-1-43f97cfb6faa4.m4a" length="27145031" type="audio/x-m4a"/>
			<itunes:summary>220 summary</itunes:summary>
			<itunes:explicit>No</itunes:explicit>
			<itunes:duration>00:53:09</itunes:duration>
			<itunes:image href="https://d3t3ozftmdmh3i.cloudfront.net/production/podcast_uploaded/998960/998960-1535212397504-93ed2911e3e38.jpg"/>
			<itunes:episodeType>full</itunes:episodeType>
		</item>
		<item>
			<title><![CDATA[220 title]]></title>
			<description><![CDATA[220 desc]]></description>
			<link>https://anchor.fm/karino2/episodes/220-e1rhbj6</link>
			<guid isPermaLink="false">0caee028-2b59-445b-a6a9-02afb998409d</guid>
			<dc:creator><![CDATA[Kazuma Arino]]></dc:creator>
			<pubDate>Wed, 30 Nov 2022 11:00:06 GMT</pubDate>
			<enclosure url="https://anchor.fm/s/68ce140/podcast/play/61434918/https%3A%2F%2Fd3ctxlq1ktw2nl.cloudfront.net%2Fproduction%2F2022-10-30%2F300132704-44100-1-43f97cfb6faa4.m4a" length="27145031" type="audio/x-m4a"/>
			<itunes:summary>220 summary</itunes:summary>
			<itunes:explicit>No</itunes:explicit>
			<itunes:duration>00:53:09</itunes:duration>
			<itunes:image href="https://d3t3ozftmdmh3i.cloudfront.net/production/podcast_uploaded/998960/998960-1535212397504-93ed2911e3e38.jpg"/>
			<itunes:episodeType>full</itunes:episodeType>
		</item>
	</channel>
</rss>
 """, ResolutionFolder=__SOURCE_DIRECTORY__>

let rss_items = AnchorRss.Load( __SOURCE_DIRECTORY__ + "/" + podcast_rss_path )

let items = rss_items.Channel.Items

items[0].Title
items[0].PubDate
items[0].Description

items[0].PubDate.DateTime

type RssItem = {
    Title: string
    PubDate: DateTime
    Description: string
}

let xmlItem2RssItem (item:AnchorRss.Item ) =
    { RssItem.Title=item.Title; PubDate = item.PubDate.LocalDateTime; Description = item.Description }

xmlItem2RssItem items[0]

let rssItem2md (rssItem:RssItem) =
    sprintf "### %s\n*%s*\n\n```\n%s\n```\n\n" rssItem.Title (rssItem.PubDate.ToString "yyyy-MM-dd HH:mm:ss") rssItem.Description


rssItem2md (xmlItem2RssItem items[0])


let podcast_md_path = $"{Common.unimemoRoot}/md/Anchor_md/anchor.md"

let mds = items |> Array.map xmlItem2RssItem
                          |> Array.map rssItem2md

File.WriteAllLines( podcast_md_path, mds )

