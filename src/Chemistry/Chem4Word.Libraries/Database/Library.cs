// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Telemetry;
using Ionic.Zip;

namespace Chem4Word.Libraries.Database
{
    public class Library
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private readonly TelemetryWriter _telemetry;
        private readonly LibraryOptions _options;

        private readonly SdFileConverter _sdFileConverter;
        private readonly CMLConverter _cmlConverter;

        public Library(TelemetryWriter telemetry, LibraryOptions options)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _telemetry = telemetry;
            _options = options;

            string libraryTarget = Path.Combine(_options.ProgramDataPath, Constants.LibraryFileName);

            _sdFileConverter = new SdFileConverter();
            _cmlConverter = new CMLConverter();

            if (!File.Exists(libraryTarget))
            {
                _telemetry.Write(module, "Information", "Copying initial Library database");
                Stream stream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "EssentialOils.zip");
                if (stream != null)
                {
                    using (ZipFile zip = ZipFile.Read(stream))
                    {
                        // Note: The original filename in EssentialOils.zip is Library.db
                        zip.ExtractAll(_options.ProgramDataPath, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
            }
        }

        private SQLiteConnection LibraryConnection()
        {
            string path = Path.Combine(_options.ProgramDataPath, Constants.LibraryFileName);
            // Source https://www.connectionstrings.com/sqlite/
            var conn = new SQLiteConnection($"Data Source={path};Synchronous=Full");
            return conn.OpenAndReturn();
        }

        public Dictionary<string, int> GetLibraryNames()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            var allNames = new Dictionary<string, int>();

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                using (SQLiteConnection conn = LibraryConnection())
                {
                    using (SQLiteDataReader names = GetAllNames(conn))
                    {
                        while (names.Read())
                        {
                            string name = names["Name"] as string;
                            if (!string.IsNullOrEmpty(name) && name.Length > 3)
                            {
                                // Exclude any purely numeric names
                                long numeric = -1;
                                if (!long.TryParse(name, out numeric))
                                {
                                    int id = int.Parse(names["ChemistryId"].ToString());
                                    if (!allNames.ContainsKey(name))
                                    {
                                        allNames.Add(name, id);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }

            sw.Stop();
            _telemetry.Write(module, "Timing", $"Reading {allNames.Count} Chemical names took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms");
            return allNames;
        }

        public void DeleteAllChemistry()
        {
            using (SQLiteConnection conn = LibraryConnection())
            {
                DeleteAllChemistry(conn);
            }
        }

        public SQLiteTransaction StartTransaction()
        {
            var conn = LibraryConnection();
            return conn.BeginTransaction();
        }

        public void EndTransaction(SQLiteTransaction transaction, bool rollback)
        {
            var conn = transaction.Connection;
            if (rollback)
            {
                transaction.Rollback();
            }
            else
            {
                transaction.Commit();
            }
            conn.Close();
            conn.Dispose();
        }

        public bool ImportCml(string cmlFile, SQLiteTransaction transaction, bool calculateProperties = false)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            bool result = false;

            try
            {
                Model model = null;

                if (cmlFile.StartsWith("<"))
                {
                    model = _cmlConverter.Import(cmlFile);
                }
                if (cmlFile.Contains("M  END"))
                {
                    model = _sdFileConverter.Import(cmlFile);
                }

                if (model != null)
                {
                    var outcome = model.EnsureBondLength(_options.PreferredBondLength, _options.SetBondLengthOnImport);
                    if (_options.RemoveExplicitHydrogensOnImport)
                    {
                        model.RemoveExplicitHydrogens();
                    }
                    if (!string.IsNullOrEmpty(outcome))
                    {
                        _telemetry.Write(module, "Information", outcome);
                    }

                    if (model.TotalAtomsCount > 0
                        || model.TotalBondsCount > 0 && model.MeanBondLength > 0)
                    {
                        if (calculateProperties)
                        {
                            var newMolecules = model.GetAllMolecules();
                            var pc = new WebServices.PropertyCalculator(_telemetry, _options.ParentTopLeft, _options.Chem4WordVersion);
                            pc.CalculateProperties(newMolecules);
                        }

                        model.CustomXmlPartGuid = "";

                        string chemicalName = model.ConciseFormula;
                        var mol = model.Molecules.Values.First();
                        if (mol.Names.Count > 0)
                        {
                            foreach (var name in mol.Names)
                            {
                                long temp;
                                if (!long.TryParse(name.Value, out temp))
                                {
                                    chemicalName = name.Value;
                                    break;
                                }
                            }
                        }

                        var conn = transaction.Connection;

                        var id = AddChemistry(conn, model, chemicalName, model.ConciseFormula);
                        foreach (var name in mol.Names)
                        {
                            AddChemicalName(conn, id, name.Value, name.FullType);
                        }

                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }

            return result;
        }

        public void UpdateChemistry(long id, string name, string xml, string formula)
        {
            using (SQLiteConnection conn = LibraryConnection())
            {
                UpdateChemistry(conn, id, name, xml, formula);
            }
        }

        private void UpdateChemistry(SQLiteConnection conn, long id, string name, string xml, string formula)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Byte[] blob = Encoding.UTF8.GetBytes(xml);

                var sb = new StringBuilder();
                sb.AppendLine("UPDATE GALLERY");
                sb.AppendLine("SET Name = @name, Chemistry = @blob, Formula = @formula");
                sb.AppendLine("WHERE ID = @id");

                var command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@id", DbType.Int64).Value = id;
                command.Parameters.Add("@blob", DbType.Binary, blob.Length).Value = blob;
                command.Parameters.Add("@name", DbType.String, name?.Length ?? 0).Value = name ?? "";
                command.Parameters.Add("@formula", DbType.String, formula?.Length ?? 0).Value = formula ?? "";

                using (SQLiteTransaction tr = conn.BeginTransaction())
                {
                    command.ExecuteNonQuery();
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        public void DeleteChemistry(long chemistryId)
        {
            using (SQLiteConnection conn = LibraryConnection())
            {
                DeleteChemistry(conn, chemistryId);
            }
        }

        private void DeleteChemistry(SQLiteConnection conn, long chemistryId)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var sb = new StringBuilder();

                using (SQLiteTransaction tr = conn.BeginTransaction())
                {
                    sb.AppendLine("DELETE FROM Gallery");
                    sb.AppendLine("WHERE ID = @id");

                    var command = new SQLiteCommand(sb.ToString(), conn);
                    command.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
                    command.ExecuteNonQuery();

                    sb = new StringBuilder();
                    sb.AppendLine("DELETE FROM ChemicalNames");
                    sb.AppendLine("WHERE ChemistryId = @id");

                    var nameCommand = new SQLiteCommand(sb.ToString(), conn);
                    nameCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
                    nameCommand.ExecuteNonQuery();

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        public string GetChemistryById(long id)
        {
            string result = null;
            using (SQLiteConnection conn = LibraryConnection())
            {
                SQLiteDataReader chemistry = GetChemistryById(conn, id);
                while (chemistry.Read())
                {
                    result = CmlFromBytes(chemistry["Chemistry"] as Byte[]);
                    break;
                }

                chemistry.Close();
                chemistry.Dispose();
            }

            return result;
        }

        private SQLiteDataReader GetChemistryById(SQLiteConnection conn, long id)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Id, Chemistry, Name, Formula");
                sb.AppendLine("FROM Gallery");
                sb.AppendLine("WHERE ID = @id");
                sb.AppendLine("ORDER BY NAME");

                var command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@id", DbType.Int64).Value = id;
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        // Called by LibraryVieModel.AddNewChemistry()
        public long AddChemistry(Model model, string chemistryName, string formula)
        {
            long result;
            using (SQLiteConnection conn = LibraryConnection())
            {
                result = AddChemistry(conn, model, chemistryName, formula);
            }

            return result;
        }

        // Called by ImportCml(), ImportCml()
        private long AddChemistry(SQLiteConnection conn, Model model, string chemistryName, string formula)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                long lastId;
                var sb = new StringBuilder();

                var blob = Encoding.UTF8.GetBytes(_cmlConverter.Export(model, true));
                //var blob = Encoding.UTF8.GetBytes(_sdFileConverter.Export(model))

                sb.AppendLine("INSERT INTO GALLERY");
                sb.AppendLine(" (Chemistry, Name, Formula)");
                sb.AppendLine("VALUES");
                sb.AppendLine(" (@blob, @name, @formula)");

                var command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@blob", DbType.Binary, blob.Length).Value = blob;
                command.Parameters.Add("@name", DbType.String, chemistryName.Length).Value = chemistryName;
                command.Parameters.Add("@formula", DbType.String, formula.Length).Value = formula;

                command.ExecuteNonQuery();
                string sql = "SELECT last_insert_rowid()";
                var cmd = new SQLiteCommand(sql, conn);
                lastId = (Int64)cmd.ExecuteScalar();

                return lastId;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return -1;
            }
        }

        private void AddChemicalName(SQLiteConnection conn, long id, string name, string dictRef)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var refs = dictRef.Split(':');

                var sb = new StringBuilder();
                sb.AppendLine("DELETE FROM ChemicalNames");
                sb.AppendLine("WHERE ChemistryID = @chemID");
                sb.AppendLine(" AND Namespace = @namespace");
                sb.AppendLine(" AND Tag = @tag");

                var deleteCommand = new SQLiteCommand(sb.ToString(), conn);
                deleteCommand.Parameters.Add("@namespace", DbType.String, refs[0].Length).Value = refs[0];
                deleteCommand.Parameters.Add("@tag", DbType.String, refs[1].Length).Value = refs[1];
                deleteCommand.Parameters.Add("@chemID", DbType.Int32).Value = id;

                sb = new StringBuilder();
                sb.AppendLine("INSERT INTO ChemicalNames");
                sb.AppendLine(" (ChemistryID, Name, Namespace, tag)");
                sb.AppendLine("VALUES");
                sb.AppendLine("(@chemID, @name, @namespace, @tag)");

                var insertCommand = new SQLiteCommand(sb.ToString(), conn);
                insertCommand.Parameters.Add("@name", DbType.String, name.Length).Value = name;
                insertCommand.Parameters.Add("@namespace", DbType.String, refs[0].Length).Value = refs[0];
                insertCommand.Parameters.Add("@tag", DbType.String, refs[1].Length).Value = refs[1];
                insertCommand.Parameters.Add("@chemID", DbType.Int32).Value = id;

                deleteCommand.ExecuteNonQuery();
                insertCommand.ExecuteNonQuery();

                // This is how to get the last inserted row's Id if we need to
                //string sql = "SELECT last_insert_rowid()"
                //var cmd = new SQLiteCommand(sql, conn)
                //lastID = (Int64)cmd.ExecuteScalar()
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void DeleteAllChemistry(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var command1 = new SQLiteCommand("DELETE FROM ChemistryByTags", conn);
                var command2 = new SQLiteCommand("DELETE FROM Gallery", conn);
                var command3 = new SQLiteCommand("DELETE FROM ChemicalNames", conn);
                var command4 = new SQLiteCommand("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME='Gallery'", conn);
                var command5 = new SQLiteCommand("VACUUM", conn);

                using (SQLiteTransaction tr = conn.BeginTransaction())
                {
                    command1.ExecuteNonQuery();
                    command2.ExecuteNonQuery();
                    command3.ExecuteNonQuery();
#if DEBUG
                    command4.ExecuteNonQuery();
#endif
                    tr.Commit();
                    command5.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private SQLiteDataReader GetAllNames(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT DISTINCT Name, ChemistryId");
                sb.AppendLine("FROM ChemicalNames");
                sb.AppendLine(" UNION");
                sb.AppendLine("SELECT DISTINCT Name, Id");
                sb.AppendLine("FROM Gallery");

                var command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        /// <summary>
        /// This is called via Microsoft.Office.Tools.CustomTaskPanel.OnVisibleChanged and Chem4Word.CustomRibbon.OnShowLibraryClick
        /// </summary>
        /// <returns></returns>
        public List<ChemistryDTO> GetAllChemistry()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var results = new List<ChemistryDTO>();

            var sw = new Stopwatch();
            sw.Start();

            using (SQLiteConnection conn = LibraryConnection())
            {
                SQLiteDataReader chemistry = GetAllChemistry(conn);
                while (chemistry.Read())
                {
                    var dto = new ChemistryDTO
                              {
                                  Id = (long) chemistry["ID"],
                                  Cml = CmlFromBytes(chemistry["Chemistry"] as Byte[]),
                                  Name = chemistry["name"] as string,
                                  Formula = chemistry["formula"] as string
                              };

                    results.Add(dto);
                }

                chemistry.Close();
                chemistry.Dispose();
            }

            sw.Stop();
            _telemetry.Write(module, "Timing", $"Reading {results.Count} structures took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms");

            return results;
        }

        private string CmlFromBytes(byte[] byteArray)
        {
            var data = Encoding.UTF8.GetString(byteArray);

            if (data.StartsWith("<"))
            {
                // Looks like cml so return it as is
                return data;
            }

            if (data.Contains("M  END"))
            {
                // Looks like a MOLFile, so convert it to CML
                return _cmlConverter.Export(_sdFileConverter.Import(data), true);
            }

            return null;
        }

        public List<UserTagDTO> GetAllUserTags()
        {
            var results = new List<UserTagDTO>();
            using (SQLiteConnection conn = LibraryConnection())
            {
                SQLiteDataReader allTags = GetAllUserTags(conn);

                while (allTags.Read())
                {
                    var tag = new UserTagDTO
                              {
                                  Id = (long) allTags["ID"],
                                  Text = (string) allTags["UserTag"],
                                  Lock = (long) allTags["Lock"]
                              };
                    results.Add(tag);
                }

                allTags.Close();
                allTags.Dispose();
            }

            return results;
        }

        public List<UserTagDTO> GetAllUserTags(int chemistryID)
        {
            var results = new List<UserTagDTO>();
            using (SQLiteConnection conn = LibraryConnection())
            {
                SQLiteDataReader allTags = GetAllUserTags(conn, chemistryID);

                while (allTags.Read())
                {
                    var tag = new UserTagDTO
                              {
                                  Id = (long)allTags["ID"],
                                  Text = (string)allTags["UserTag"],
                                  Lock = (long)allTags["Lock"]
                              };
                    results.Add(tag);
                }

                allTags.Close();
                allTags.Dispose();
            }

            return results;
        }

        private SQLiteDataReader GetAllUserTags(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Id, UserTag, Lock");
                sb.AppendLine("FROM UserTags");

                var command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        private SQLiteDataReader GetAllUserTags(SQLiteConnection conn, int chemistryId)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SELECT Id, UserTag, Lock");
                sb.AppendLine("FROM UserTags");
                sb.AppendLine("WHERE ID IN");
                sb.AppendLine(" (SELECT TagID");
                sb.AppendLine("  FROM ChemistryByTags");
                sb.AppendLine("  WHERE GalleryID = @id)");

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@id", DbType.Int64).Value = chemistryId;
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        public List<ChemistryTagDTO> GetChemistryByTags()
        {
            var results = new List<ChemistryTagDTO>();
            using (SQLiteConnection conn = LibraryConnection())
            {
                SQLiteDataReader allTags = GetChemistryByTags(conn);
                while (allTags.Read())
                {
                    var dto = new ChemistryTagDTO
                              {
                                  Id = (long)allTags["ID"],
                                  GalleryId = (long)allTags["GalleryID"],
                                  TagId = (long)allTags["TagID"]
                              };

                    results.Add(dto);
                }

                allTags.Close();
                allTags.Dispose();
            }

            return results;
        }

        private SQLiteDataReader GetChemistryByTags(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Id, GalleryID, TagID");
                sb.AppendLine("FROM ChemistryByTags");
                sb.AppendLine("ORDER BY GalleryID, TagID");

                var command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        private SQLiteDataReader GetAllChemistry(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var sb = new StringBuilder();

                sb.AppendLine("SELECT Id, Chemistry, Name, Formula");
                sb.AppendLine("FROM Gallery");
                sb.AppendLine("ORDER BY Name");

                var command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        private SQLiteDataReader GetAllChemistryWithTags(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Id, Chemistry, Name, Formula, UserTag");
                sb.AppendLine("FROM GetAllChemistryWithTags");
                sb.AppendLine("ORDER BY Name");

                var command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        private SQLiteDataReader GetChemicalNames(SQLiteConnection conn, int id)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT ChemicalNameID, Name, Namespace, Tag");
                sb.AppendLine("FROM ChemicalNames");
                sb.AppendLine("WHERE ChemistryID = @id");
                sb.AppendLine("ORDER BY Name");

                var command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@id", DbType.Int64).Value = id;
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }
    }
}