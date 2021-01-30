module Common

open System.IO

let unimemoRoot ="../../../Google ドライブ/notes"
let mdroot = $"{unimemoRoot}"

let ensureDir (di: DirectoryInfo) =
  if not di.Exists then
      Directory.CreateDirectory di.FullName |> ignore

let saveText (fi:FileInfo) text =
  File.WriteAllText(fi.FullName, text)
