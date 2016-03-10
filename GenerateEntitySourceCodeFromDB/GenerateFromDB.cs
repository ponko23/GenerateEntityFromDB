using Dapper;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace GenerateEntityFromDB
{
    /// <summary>
    /// 
    /// </summary>
    public static class GenerateFromDB
    {
        /// <summary>
        /// DBからTable定義情報を取得します。
        /// </summary>
        /// <param name="connectionString">DB接続文字列</param>
        /// <returns>Table定義情報</returns>
        public static TableInfo[] GetTableInfos(string connectionString)
        {
            using (var db = new SqlConnection(connectionString))
            {
                var tableNameList = db.Query<string>("select name from sys.tables");
                if (tableNameList == null || tableNameList.Count() == 0) return null;
                var tableList = new List<TableInfo>();
                foreach (var tableName in tableNameList)
                {
                    tableList.Add(new TableInfo
                    {
                        Name = tableName,
                        Columns = db.Query<ColumnInfo>(@"
select
  col.name Name,
  typ.name TypeName,
  case typ.name
	when 'nvarchar' then col.max_length / 2
	when 'varchar' then col.max_length
	when 'decimal' then col.precision 
	else null
  end Length,
  col.scale MinorityDigit,
  convert(bit, col.is_nullable) IsNullable,
  convert(bit, col.is_identity) IsIdentity,
  col.default_object_id DefaultValue,
  case
    when ic.key_ordinal is null then convert(bit, 0)
    else convert(bit, 1)
  end IsPK,
  ic.key_ordinal KeyOrdinal
from
  sys.tables tbl
inner join
  sys.columns col
on tbl.object_id = col.object_id
inner join
  sys.types typ
on col.user_type_id = typ.user_type_id
left outer join
  sys.indexes i
on tbl.object_id = i.object_id
and i.is_primary_key = 1
left outer join
  sys.index_columns ic
on tbl.object_id = ic.object_id
and col.column_id = ic.column_id
and i.index_id = ic.index_id
where
  tbl.object_id = object_id(@tableName)
order by
  tbl.object_id,
  col.column_id
", new { tableName = tableName }).ToArray()
                    });
                }
                return tableList.ToArray();
            }
        }
    }

    /// <summary>DBテーブル情報のクラスです。</summary>
    public class TableInfo
    {
        /// <summary>テーブル名を取得または設定します。</summary>
        public string Name { get; set; }
        /// <summary>列情報を取得または設定します。</summary>
        public ColumnInfo[] Columns { get; set; }
    }

    /// <summary>DB列情報のクラスです。</summary>
    public class ColumnInfo
    {
        /// <summary>列名を取得または設定します。</summary>
        public string Name { get; set; }
        /// <summary>型名を取得または設定します。</summary>
        public string TypeName { get; set; }
        /// <summary>最大桁数を取得または設定します。</summary>
        public int MaxLength { get; set; }
        /// <summary>Decimal型の小数点以下の桁数を取得または設定します</summary>
        public int MinorityDigit { get; set; }
        /// <summary>null許容フラグを取得または設定します。</summary>
        public bool IsNullable { get; set; }
        /// <summary>自動採番フラグを取得または設定します。</summary>
        public bool IsIdentity { get; set; }
        /// <summary>初期値を取得または設定します。</summary>
        public string DefaultValue { get; set; }
        /// <summary>PrimaryKeyフラグを取得または設定します。</summary>
        public bool IsPK { get; set; }
        /// <summary>PrimaryKeyの順番を取得または設定します。</summary>
        public int? KeyOrdinal { get; set; }
    }
}
