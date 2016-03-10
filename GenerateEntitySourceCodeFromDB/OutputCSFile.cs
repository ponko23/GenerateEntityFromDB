using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GenerateEntityFromDB
{
    /// <summary></summary>
    public class OutputCSFile
    {
        /// <summary></summary>
        public string OutputDirectoryPath { get; set; }

        /// <summary></summary>
        public string NameSpace { get; set; }

        /// <summary></summary>
        private Dictionary<string, string> typeNameDictionary = new Dictionary<string, string>();

        /// <summary>OutputCSFileクラスのインスタンスを初期化します</summary>
        /// <param name="nameSpace"></param>
        /// <param name="outputDirectoryPath"></param>
        public OutputCSFile(string nameSpace, string outputDirectoryPath)
        {
            this.NameSpace = nameSpace;
            this.OutputDirectoryPath = outputDirectoryPath;
            this.SetTypeNameDictionary();
        }

        /// <summary>テーブル定義情報からデータモデルのソースコードを作成します。</summary>
        /// <param name="info">テーブル定義情報</param>
        /// <returns>データモデルソースコード</returns>
        public string GenerateEntityClassSourceCode(TableInfo info)
        {
            List<string> output = new List<string>();
            // 先頭部作成
            output.Add($@"using System;

");
            if (!string.IsNullOrWhiteSpace(this.NameSpace))
            {
                output.Add($@"namespace {this.NameSpace}
{{");
            }

            output.Add($@"
    /// <summary>{info.Name}のクラスです。</summary>
    public class {info.Name} : IEntity
    {{
#region テーブルの列に対応したプロパティ");

            // プロパティ作成
            output.AddRange(info.Columns.Select(s => $@"
        /// <summary>{s.Name}を取得または設定します</summary>
        public {this.typeNameDictionary[s.TypeName]}{(s.IsNullable && this.typeNameDictionary[s.TypeName] != "string" ? "?" : "")} {s.Name} {{ get; set; }}
"));

            output.Add(@"
#endregion

#region 汎用クエリ文字列");

            // クエリ作成
            output.Add($@"
        /// <summary>レコードを全件取得するクエリを取得します</summary>
        public string SelectAllQuery()
        {{
            return @""
select
    {string.Join("\r\n   ,", info.Columns.Select(s => $"{s.Name}"))}
from
    {info.Name}
"";
        }}
");

            output.Add($@"
        /// <summary>PrymaryKeyを指定して、レコードを一件取得するクエリを取得します</summary>
        public string SelectWithKeyQuery()
        {{
            return @""
select
    {string.Join("\r\n   ,", info.Columns.Select(s => $"{s.Name}"))}
from
    {info.Name}
where
    {string.Join("\r\nand ", info.Columns.Where(w => w.IsPK).OrderBy(o => o.KeyOrdinal).Select(s => $"{s.Name} = @{s.Name}"))}
"";
        }}
");

            output.Add($@"
        /// <summary>指定条件に一致するレコードを取得するクエリを取得します</summary>
        public string SelectQuery()
        {{
            return @""
select
    {string.Join("\r\n   ,", info.Columns.Select(s => $"{s.Name}"))}
from
    {info.Name}
where
    {string.Join("\r\nand ", info.Columns.Where(w => !w.IsPK).Select(s => $"({s.Name} = @{s.Name} or @{s.Name} = null)"))}
"";
        }}
");

            output.Add($@"
        /// <summary>レコードを一件挿入するクエリを取得します</summary>
        public string InsertQuery()
        {{
            return @""
insert into {info.Name}
(
    {string.Join("\r\n   ,", info.Columns.Select(s => $"{s.Name}"))}
)
values(
    {string.Join("\r\n   ,", info.Columns.Select(s => $"@{s.Name}"))}
)"";
        }}
");

            output.Add($@"
        /// <summary>レコードを一件削除するクエリを取得します</summary>
        public string DeleteWithKeyQuery()
        {{
            return @""
delete from
    {info.Name}
where
    {string.Join("\r\nand ", info.Columns.Where(w => w.IsPK).OrderBy(o => o.KeyOrdinal).Select(s => $"{s.Name} = @{s.Name}"))}
"";
        }}
");

            output.Add($@"
        /// <summary>指定条件に一致するレコードを削除するクエリを取得します</summary>
        public string DeleteQuery()
        {{
            return @""
delete from
    {info.Name}
where
    {string.Join("\r\nand ", info.Columns.Where(w => !w.IsPK).Select(s => $"({s.Name} = @{s.Name} or @{s.Name} = null)"))}
"";
        }}");

            // クラス・名前空間を閉じる
            output.Add(@"
#endregion
    }");
            if (!string.IsNullOrWhiteSpace(this.NameSpace))
            {
                output.Add(@"
}
");
            }
            return string.Join(string.Empty, output);
        }


        /// <summary></summary>
        /// <returns></returns>
        public string GenerateDapperExtentionClassSourceCode()
        {
            var output = new List<string>();
            output.Add(@"using Dapper;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

");
            if (!string.IsNullOrWhiteSpace(this.NameSpace))
            {
                output.Add($@"namespace {this.NameSpace}
{{");
            }

            output.Add(@"
    /// <summary>IEntityを実装するEntityクラスのテーブルを操作するDapper用拡張メソッドクラスです</summary>
    public static class DapperExtentions
    {
        /// <summary>対象テーブルのレコードを全件取得します</summary>
        /// <typeparam name=""T"">Entityクラス</typeparam>
        /// <param name=""connection"">データベース接続</param>
        /// <returns>対象Entityの配列</returns>
        public static T[] SelectAll<T>(this DbConnection connection) where T : IEntity, new()
        {
            return connection.Query<T>(new T().GetSelectAllQuery()).ToArray();
        }

        /// <summary>対象テーブルからPrimaryKeyで指定したレコードを一件取得します</summary>
        /// <typeparam name=""T"">Entityクラス</typeparam>
        /// <param name=""connection"">データベース接続</param>
        /// <param name=""conditionData"">検索条件</param>
        /// <returns>対象Entity</returns>
        public static T SelectOne<T>(this DbConnection connection, T conditionData) where T : IEntity
        {
            return connection.Query<T>(conditionData.GetSelectWithKeyQuery(), conditionData).FirstOrDefault();
        }

        /// <summary>対象テーブルから指定した条件に一致するレコードを取得します</summary>
        /// <typeparam name=""T"">Entityクラス</typeparam>
        /// <param name=""connection"">データベース接続</param>
        /// <param name=""conditionData"">検索条件</param>
        /// <returns>対象Entityの配列</returns>
        public static T[] Select<T>(this DbConnection connection, T conditionData) where T : IEntity
        {
            return connection.Query<T>(conditionData.GetSelectQuery(), conditionData).ToArray();
        }

        /// <summary>対象テーブルにレコードを一件挿入します</summary>
        /// <typeparam name=""T"">Entityクラス</typeparam>
        /// <param name=""connection"">データベース接続</param>
        /// <param name=""insertData"">挿入するデータ</param>
        /// <returns>挿入件数</returns>
        public static int Insert<T>(this DbConnection connection, T insertData) where T : IEntity
        {
            return connection.Execute(insertData.GetInsertQuery(), insertData);
        }

        // updateは都度書いた方が良いので無し

        /// <summary>対象テーブルからPrimaryKeyで指定したレコードを一件削除します</summary>
        /// <typeparam name=""T"">Entityクラス</typeparam>
        /// <param name=""connection"">データベース接続</param>
        /// <param name=""deleteData"">削除条件</param>
        /// <returns>削除件数</returns>
        public static int DeleteWithKey<T>(this DbConnection connection, T deleteData) where T : IEntity
        {
            return connection.Execute(deleteData.GetDeleteWithKeyQuery(), deleteData);
        }

        /// <summary>対象テーブルから指定した条件に一致するレコードを削除します</summary>
        /// <typeparam name=""T"">Entityクラス</typeparam>
        /// <param name=""connection"">データベース接続</param>
        /// <param name=""deleteData"">削除条件</param>
        /// <returns>削除件数</returns>
        public static int Delete<T>(this DbConnection connection, T deleteData) where T : IEntity
        {
            return connection.Execute(deleteData.GetDeleteQuery(), deleteData);
        }
    }");
            if (!string.IsNullOrWhiteSpace(this.NameSpace))
            {
                output.Add(@"
}
");
            }
            return string.Join(string.Empty, output);
        }

        /// <summary></summary>
        /// <returns></returns>
        public string GenerateIEntitySourceCode()
        {
            var output = new List<string>();
            output.Add(@"using Dapper;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

");
            if (!string.IsNullOrWhiteSpace(this.NameSpace))
            {
                output.Add($@"namespace {this.NameSpace}
{{");
            }

            output.Add(@"
    /// <summary>対象テーブルへ発行するクエリを内包するインターフェース</summary>
    public interface IEntity
    {
        /// <summary>全件取得クエリ</summary>
        string GetSelectAllQuery();

        /// <summary>Key指定取得クエリ</summary>
        string GetSelectWithKeyQuery();

        /// <summary>条件検索クエリ</summary>
        string GetSelectQuery();

        /// <summary>挿入クエリ</summary>
        string GetInsertQuery();

        // updateは都度書いた方が良いので無し

        /// <summary>Key指定削除クエリ</summary>
        string GetDeleteWithKeyQuery();

        /// <summary>条件指定削除クエリ</summary>
        string GetDeleteQuery();
    }");
            if (!string.IsNullOrWhiteSpace(this.NameSpace))
            {
                output.Add(@"
}
");
            }
            return string.Join(string.Empty, output);
        }



        /// <summary></summary>
        /// <param name="text"></param>
        public void WriteFile(string filename, string text)
        {
            File.WriteAllText($"{this.OutputDirectoryPath}\\{filename}.cs", text);
        }

        /// <summary>型変換用辞書を設定します</summary>
        private void SetTypeNameDictionary()
        {
            this.typeNameDictionary.Add("nvarchar", "string");
            this.typeNameDictionary.Add("varchar", "string");
            this.typeNameDictionary.Add("nchar", "string");
            this.typeNameDictionary.Add("char", "string");
            this.typeNameDictionary.Add("int", "int");
            this.typeNameDictionary.Add("bigint", "long");
            this.typeNameDictionary.Add("numeric", "long");
            this.typeNameDictionary.Add("decimal", "decimal");
            this.typeNameDictionary.Add("datetime", "DateTime");
            this.typeNameDictionary.Add("date", "DateTime");
            this.typeNameDictionary.Add("bit", "bool");
            this.typeNameDictionary.Add("money", "decimal");
        }
    }
}