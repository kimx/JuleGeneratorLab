using System.Data.SqlClient;
using System.Data;
using System.Linq; // Required for LINQ operations like .Any()
using JuleGeneratorLab.Models; // Added using statement

namespace JuleGeneratorLab.Services
{
    // ColumnDetail class definition removed from here

    public class DatabaseSchemaReader
    {
        public List<string> GetTables(string connectionString)
        {
            var tables = new List<string>();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable schema = connection.GetSchema("Tables");
                    foreach (DataRow row in schema.Rows)
                    {
                        string? schemaName = row["TABLE_SCHEMA"] as string;
                        string? tableName = row["TABLE_NAME"] as string;
                        string? tableType = row["TABLE_TYPE"] as string;

                        // We are interested in base tables, not views or system tables
                        if (tableType == "BASE TABLE" && !string.IsNullOrEmpty(tableName))
                        {
                            // Optional: Exclude common system/diagram schemas if necessary
                            if (schemaName != "sys" && schemaName != "INFORMATION_SCHEMA")
                            {
                                tables.Add(string.IsNullOrEmpty(schemaName) || schemaName == "dbo" ? tableName : $"{schemaName}.{tableName}");
                            }
                        }
                    }
                }
                tables.Sort(); // Sort alphabetically
            }
            catch (SqlException ex)
            {
                // Log the exception or handle it as per application's error handling strategy
                // For now, rethrow or wrap it to be caught by the UI layer
                throw new Exception($"SQL Error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Catch other potential exceptions (e.g., invalid connection string format)
                throw new Exception($"Error connecting to database or retrieving tables: {ex.Message}", ex);
            }
            return tables;
        }

        public List<ColumnDetail> GetColumns(string connectionString, string tableNameWithSchema)
        {
            var columns = new List<ColumnDetail>();
            string? schemaName = "dbo"; // Default schema
            string? actualTableName = tableNameWithSchema;

            if (tableNameWithSchema.Contains("."))
            {
                var parts = tableNameWithSchema.Split('.');
                schemaName = parts[0];
                actualTableName = parts[1];
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Get Primary Key information first
                    var primaryKeys = new List<string>();
                    string pkQuery = @"
                        SELECT KCU.COLUMN_NAME
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC
                        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU
                            ON TC.CONSTRAINT_NAME = KCU.CONSTRAINT_NAME
                            AND TC.TABLE_SCHEMA = KCU.TABLE_SCHEMA
                            AND TC.TABLE_NAME = KCU.TABLE_NAME
                        WHERE TC.CONSTRAINT_TYPE = 'PRIMARY KEY'
                            AND TC.TABLE_SCHEMA = @TableSchema
                            AND TC.TABLE_NAME = @TableName;";

                    using (SqlCommand pkCommand = new SqlCommand(pkQuery, connection))
                    {
                        pkCommand.Parameters.AddWithValue("@TableSchema", schemaName);
                        pkCommand.Parameters.AddWithValue("@TableName", actualTableName);
                        using (SqlDataReader pkReader = pkCommand.ExecuteReader())
                        {
                            while (pkReader.Read())
                            {
                                primaryKeys.Add(pkReader["COLUMN_NAME"].ToString() ?? string.Empty);
                            }
                        }
                    }

                    // Get Column information
                    string query = @"
                        SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = @TableSchema AND TABLE_NAME = @TableName
                        ORDER BY ORDINAL_POSITION;";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TableSchema", schemaName);
                        command.Parameters.AddWithValue("@TableName", actualTableName);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var columnDetail = new ColumnDetail
                                {
                                    ColumnName = reader["COLUMN_NAME"].ToString() ?? string.Empty,
                                    DataType = reader["DATA_TYPE"].ToString() ?? string.Empty,
                                    IsNullable = reader["IS_NULLABLE"].ToString()?.ToUpper() == "YES",
                                    IsPrimaryKey = primaryKeys.Contains(reader["COLUMN_NAME"].ToString() ?? string.Empty)
                                };
                                // You can extend DataType to include length/precision/scale if needed
                                // For example:
                                // if (reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                                // {
                                //    columnDetail.DataType += $"({reader["CHARACTER_MAXIMUM_LENGTH"]})";
                                // }
                                // else if (reader["NUMERIC_PRECISION"] != DBNull.Value && reader["NUMERIC_SCALE"] != DBNull.Value)
                                // {
                                //    columnDetail.DataType += $"({reader["NUMERIC_PRECISION"]},{reader["NUMERIC_SCALE"]})";
                                // }
                                columns.Add(columnDetail);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"SQL Error fetching columns for table '{tableNameWithSchema}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching columns for table '{tableNameWithSchema}': {ex.Message}", ex);
            }
            return columns;
        }
    }
}
