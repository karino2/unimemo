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


