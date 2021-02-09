// fsharplint:disable Hints

#load "Common.fs"
open Common


// livejournalのダウンロード。

let downloadlj year month =
    shellExecute $"{unimemoRoot}/original/lj/ljget.sh" $"{year} %02d{month}"


downloadlj 2012 3

[4..12]
|> List.map (downloadlj 2012)

[1..12]
|> List.map (downloadlj 2013)

[1..12]
|> List.map (downloadlj 2014)

[1..12]
|> List.map (downloadlj 2015)

[1..12]
|> List.map (downloadlj 2016)

[1..12]
|> List.map (downloadlj 2017)


//
// mixiのダウンロード
//


#r "nuget: HtmlAgilityPack"
#r "nuget: HtmlAgilityPack.CssSelectors.NetCore"

open HtmlAgilityPack
open HtmlAgilityPack.CssSelectors.NetCore
open System.Text.RegularExpressions


let loadHtml path =
    let doc = HtmlDocument()
    doc.Load(path=path)
    doc

// pageは1が無いと同じ。
let listDiaryUrl year month page =
    let burl = $"https://mixi.jp/list_diary.pl?year={year}&month={month}"
    if page = 1 then
        burl
    else
        $"{burl}&page={page}"

let listFileName year month page = $"list_diary_{year}_{month}_{page}.html"

let downloadListDiaryOne year month page =
    let url = listDiaryUrl year month page
    let fname = listFileName year month page
    shellExecute "mixi_get.sh" $"{url} {fname}" |> ignore
    fname

let pagePat = Regex(".*list_diary.pl?.*page=([0-9]+).*")


let hasNext (doc:HtmlDocument) (page:int)=
    let allA = doc.QuerySelectorAll("a")
               |> Seq.filter  (fun a -> pagePat.Match(a.GetAttributeValue("href", "")).Success)
               |> Seq.map (fun a -> System.Int32.Parse(pagePat.Match(a.GetAttributeValue("href", "")).Groups.[1].Value))
               |> Seq.filter (fun num -> num > page)
               |> Seq.toList 
    allA.Length <>0

open System.Threading
let waitTime = 2000

let downloadListDiaryMonth year month =
    let rec downloadRec (fs: string list) page =
        Thread.Sleep(waitTime)
        let fname = downloadListDiaryOne year month page
        let doc = loadHtml fname
        let newFs = fname::fs
        if not (hasNext doc page) then
            newFs
        else
            downloadRec newFs (page+1)
    downloadRec [] 1

let downloadListDiaryMonthList year monthList =
    monthList
    |> List.map (downloadListDiaryMonth year)

let downloadListDiaryYear year =
    downloadListDiaryMonthList year [1..12]


// list_diary.plのダウンロード

downloadListDiaryMonthList 2004 [10..12]

[2005..2020] |> List.map downloadListDiaryYear

downloadListDiaryMonth 2021 1

// 手動で mixi/list_diary/ 下にコピー


// アドホックな試行錯誤

let fpath = $"{unimemoRoot}/original/mixi/list_diary/list_diary_2005_5_1.html"

let doc = loadHtml fpath
doc.QuerySelectorAll("a").Count

