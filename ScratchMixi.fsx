// fsharplint:disable Hints

#r "nuget: HtmlAgilityPack"
#r "nuget: HtmlAgilityPack.CssSelectors.NetCore"

// EUC用
#r "nuget: System.Text.Encoding.CodePages"

#load "Common.fs"
#load "Mixi.fs"

open Common

open Mixi.Download
open HtmlAgilityPack.CssSelectors.NetCore
open System.Text.RegularExpressions



// list_diary.plのダウンロード

downloadListDiaryMonthList 2004 [10..12]

[2005..2020] |> List.map downloadListDiaryYear

downloadListDiaryMonth 2021 1

// 手動で mixi/list_diary/ 下にコピー

// diaryのダウンロード
getOneLDDiary 2004 10 1
getOneLDDiary 2004 11 1
getOneLDDiary 2004 12 1

getOneYearDiary 2005
[2006..2021] |> List.iter getOneYearDiary


// ボイスのlvのダウンロード

saveLV "https://mixi.jp/list_voice.pl"


// ボイスのmoreのある物だけダウンロード
saveAllVoices ()


//
// アドホックな試行錯誤
//

let fis = listLV ()


let lvdoc = loadHtml fis.[0].FullName

lvdoc.QuerySelectorAll("a").Count

