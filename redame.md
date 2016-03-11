# Generate Entity Sourcecode from DB(GES_DB)

databaseに接続して、全UserTableと1対1で対応するEntityClassファイル(.cs)を生成するコンソールアプリケーションです。

---
##使用方法

1.対話式

    C:\>GES_DB.exe
    DB接続文字列を入力してください。:
    出力先ディレクトリのパスを入力してください。:
    namespaceを入力してください（省略可）:

2.コマンドライン引数

    C:\>GES_DB.exe "DB接続文字列" "出力先ディレクトリパス" ["namespace（省略可）"]

3.ヘルプ表示

    C:\>GES_DB.exe -h
    対話式
    GES_DB.exe
    コマンドライン引数指定
    GES_DB.exe "DB接続文字列" "出力先ディレクトリパス" ["namespace（省略可）"]
    ヘルプ
    GES_DB.exe -h　

