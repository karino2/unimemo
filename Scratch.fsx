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


