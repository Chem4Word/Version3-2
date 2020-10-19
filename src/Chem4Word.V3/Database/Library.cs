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
using Chem4Word.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Ionic.Zip;

namespace Chem4Word.Database
{
    public class Library
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;
        private readonly SdFileConverter _sdFileConverter;
        private readonly CMLConverter _cmlConverter;

        public Library()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string libraryTarget = Path.Combine(Globals.Chem4WordV3.AddInInfo.ProgramDataPath, Constants.LibraryFileName);

            _sdFileConverter = new SdFileConverter();
            _cmlConverter = new CMLConverter();

            if (!File.Exists(libraryTarget))
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Information", "Copying initial Library database");
                Stream stream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Library.zip");
                using (ZipFile zip = ZipFile.Read(stream))
                {
                    zip.ExtractAll(Globals.Chem4WordV3.AddInInfo.ProgramDataPath, ExtractExistingFileAction.OverwriteSilently);
                }
            }
        }

        private SQLiteConnection LibraryConnection()
        {
            string path = Path.Combine(Globals.Chem4WordV3.AddInInfo.ProgramDataPath, Constants.LibraryFileName);
            // Source https://www.connectionstrings.com/sqlite/
            var conn = new SQLiteConnection($"Data Source={path};Synchronous=Full");
            return conn.OpenAndReturn();
        }

        public Dictionary<string, int> GetLibraryNames()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            var allNames = new Dictionary<string, int>();

            Stopwatch sw = new Stopwatch();
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
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }

            sw.Stop();
            Globals.Chem4WordV3.Telemetry.Write(module, "Timing", $"Reading {allNames.Count} Chemical names took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms");
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
                    var outcome = model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength,
                                                         Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromLibrary);
                    if (Globals.Chem4WordV3.SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary)
                    {
                        model.RemoveExplicitHydrogens();
                    }
                    if (!string.IsNullOrEmpty(outcome))
                    {
                        Globals.Chem4WordV3.Telemetry.Write(module, "Information", outcome);
                    }

                    if (model.TotalAtomsCount > 0
                        || model.TotalBondsCount > 0 && model.MeanBondLength > 0)
                    {
                        if (calculateProperties)
                        {
                            var newMolecules = model.GetAllMolecules();
                            ChemistryHelper.CalculateProperties(newMolecules);
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
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("UPDATE GALLERY");
                sb.AppendLine("SET Name = @name, Chemistry = @blob, Formula = @formula");
                sb.AppendLine("WHERE ID = @id");

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
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
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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
                StringBuilder sb = new StringBuilder();

                using (SQLiteTransaction tr = conn.BeginTransaction())
                {
                    sb.AppendLine("DELETE FROM Gallery");
                    sb.AppendLine("WHERE ID = @id");

                    SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                    command.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
                    command.ExecuteNonQuery();

                    sb = new StringBuilder();
                    sb.AppendLine("DELETE FROM ChemicalNames");
                    sb.AppendLine("WHERE ChemistryId = @id");

                    SQLiteCommand nameCommand = new SQLiteCommand(sb.ToString(), conn);
                    nameCommand.Parameters.Add("@id", DbType.Int64, 20).Value = chemistryId;
                    nameCommand.ExecuteNonQuery();

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SELECT Id, Chemistry, Name, Formula");
                sb.AppendLine("FROM Gallery");
                sb.AppendLine("WHERE ID = @id");
                sb.AppendLine("ORDER BY NAME");

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@id", DbType.Int64).Value = id;
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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
                StringBuilder sb = new StringBuilder();

                Byte[] blob = Encoding.UTF8.GetBytes(_cmlConverter.Export(model, true));
                //Byte[] blob = Encoding.UTF8.GetBytes(_sdFileConverter.Export(model))

                sb.AppendLine("INSERT INTO GALLERY");
                sb.AppendLine(" (Chemistry, Name, Formula)");
                sb.AppendLine("VALUES");
                sb.AppendLine(" (@blob, @name, @formula)");

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@blob", DbType.Binary, blob.Length).Value = blob;
                command.Parameters.Add("@name", DbType.String, chemistryName.Length).Value = chemistryName;
                command.Parameters.Add("@formula", DbType.String, formula.Length).Value = formula;

                command.ExecuteNonQuery();
                string sql = "SELECT last_insert_rowid()";
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                lastId = (Int64)cmd.ExecuteScalar();

                return lastId;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return -1;
            }
        }

        private long AddChemicalName(SQLiteConnection conn, long id, string name, string dictRef)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                long lastID;
                var refs = dictRef.Split(':');

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("DELETE FROM ChemicalNames");
                sb.AppendLine("WHERE ChemistryID = @chemID");
                sb.AppendLine(" AND Namespace = @namespace");
                sb.AppendLine(" AND Tag = @tag");

                SQLiteCommand delcommand = new SQLiteCommand(sb.ToString(), conn);
                delcommand.Parameters.Add("@namespace", DbType.String, refs[0].Length).Value = refs[0];
                delcommand.Parameters.Add("@tag", DbType.String, refs[1].Length).Value = refs[1];
                delcommand.Parameters.Add("@chemID", DbType.Int32).Value = id;

                sb = new StringBuilder();
                sb.AppendLine("INSERT INTO ChemicalNames");
                sb.AppendLine(" (ChemistryID, Name, Namespace, tag)");
                sb.AppendLine("VALUES");
                sb.AppendLine("(@chemID, @name, @namespace, @tag)");

                SQLiteCommand insertCommand = new SQLiteCommand(sb.ToString(), conn);
                insertCommand.Parameters.Add("@name", DbType.String, name.Length).Value = name;
                insertCommand.Parameters.Add("@namespace", DbType.String, refs[0].Length).Value = refs[0];
                insertCommand.Parameters.Add("@tag", DbType.String, refs[1].Length).Value = refs[1];
                insertCommand.Parameters.Add("@chemID", DbType.Int32).Value = id;

                delcommand.ExecuteNonQuery();
                insertCommand.ExecuteNonQuery();
                string sql = "SELECT last_insert_rowid()";
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                lastID = (Int64)cmd.ExecuteScalar();

                return lastID;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return -1;
            }
        }

        private void DeleteAllChemistry(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                SQLiteCommand command = new SQLiteCommand("DELETE FROM ChemistryByTags", conn);
                SQLiteCommand command2 = new SQLiteCommand("DELETE FROM Gallery", conn);
                SQLiteCommand command3 = new SQLiteCommand("DELETE FROM ChemicalNames", conn);
                SQLiteCommand command4 = new SQLiteCommand("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME='Gallery'", conn);
                SQLiteCommand command5 = new SQLiteCommand("VACUUM", conn);

                using (SQLiteTransaction tr = conn.BeginTransaction())
                {
                    command.ExecuteNonQuery();
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
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SELECT DISTINCT Name, ChemistryId");
                sb.AppendLine("FROM ChemicalNames");
                sb.AppendLine(" UNION");
                sb.AppendLine("SELECT DISTINCT Name, Id");
                sb.AppendLine("FROM Gallery");

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        /// <summary>
        /// This is called via Microsoft.Office.Tools.CustomTaskPanel.OnVisibleChanged and Chem4Word.CustomRibbon.OnShowLibraryClick
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public List<ChemistryDTO> GetAllChemistry(string filter)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}({filter})";

            var results = new List<ChemistryDTO>();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            using (SQLiteConnection conn = LibraryConnection())
            {
                SQLiteDataReader chemistry = GetAllChemistry(conn, filter);
                while (chemistry.Read())
                {
                    var dto = new ChemistryDTO();

                    dto.Id = (long)chemistry["ID"];
                    dto.Cml = CmlFromBytes(chemistry["Chemistry"] as Byte[]);
                    dto.Name = chemistry["name"] as string;
                    dto.Formula = chemistry["formula"] as string;

                    results.Add(dto);
                }

                chemistry.Close();
                chemistry.Dispose();
            }

            sw.Stop();
            Globals.Chem4WordV3.Telemetry.Write(module, "Timing", $"Reading {results.Count} structures took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms");

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

        public List<ChemistryTagDTO> GetChemistryByTags()
        {
            List<ChemistryTagDTO> results = new List<ChemistryTagDTO>();
            using (SQLiteConnection conn = LibraryConnection())
            {
                SQLiteDataReader allTags = GetChemistryByTags(conn);
                while (allTags.Read())
                {
                    ChemistryTagDTO dto = new ChemistryTagDTO();

                    dto.Id = (long)allTags["ID"];
                    dto.GalleryId = (long)allTags["GalleryID"];
                    dto.TagId = (long)allTags["TagID"];

                    results.Add(dto);
                }

                allTags.Close();
                allTags.Dispose();
            }

            return results;
        }

        public List<UserTagDTO> GetAllUserTags()
        {
            List<UserTagDTO> results = new List<UserTagDTO>();
            using (SQLiteConnection conn = LibraryConnection())
            {
                SQLiteDataReader allTags = GetAllUserTags(conn);

                while (allTags.Read())
                {
                    var tag = new UserTagDTO();
                    tag.Id = (long)allTags["ID"];
                    tag.Text = (string)allTags["UserTag"];
                    tag.Lock = (long)allTags["Lock"];
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
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SELECT Id, UserTag, Lock");
                sb.AppendLine("FROM UserTags");

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        public List<UserTagDTO> GetAllUserTags(int chemistryID)
        {
            List<UserTagDTO> results = new List<UserTagDTO>();
            using (SQLiteConnection conn = LibraryConnection())
            {
                SQLiteDataReader allTags = GetAllUserTags(conn, chemistryID);

                while (allTags.Read())
                {
                    var tag = new UserTagDTO();
                    tag.Id = (long)allTags["ID"];
                    tag.Text = (string)allTags["UserTag"];
                    tag.Lock = (long)allTags["Lock"];
                    results.Add(tag);
                }

                allTags.Close();
                allTags.Dispose();
            }

            return results;
        }

        public static SQLiteDataReader GetChemistryByTags(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SELECT Id, GalleryID, TagID");
                sb.AppendLine("FROM ChemistryByTags");
                sb.AppendLine("ORDER BY GalleryID, TagID");

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }

        private SQLiteDataReader GetAllChemistry(SQLiteConnection conn, string filter)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                StringBuilder sb = new StringBuilder();

                if (string.IsNullOrWhiteSpace(filter))
                {
                    sb.AppendLine("SELECT Id, Chemistry, Name, Formula");
                    sb.AppendLine("FROM Gallery");
                    sb.AppendLine("ORDER BY Name");

                    SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                    return command.ExecuteReader();
                }
                else
                {
                    sb.AppendLine("SELECT Id, Chemistry, Name, Formula");
                    sb.AppendLine("FROM Gallery");
                    sb.AppendLine("WHERE Name LIKE @filter");
                    sb.AppendLine("OR");
                    sb.AppendLine("Id IN");
                    sb.AppendLine("(SELECT ChemistryID");
                    sb.AppendLine(" FROM ChemicalNames");
                    sb.AppendLine(" WHERE Name LIKE @filter)");
                    sb.AppendLine("ORDER BY Name");

                    SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                    command.Parameters.Add("@filter", DbType.String).Value = $"%{filter}%";
                    return command.ExecuteReader();
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SELECT Id, Chemistry, Name, Formula, UserTag");
                sb.AppendLine("FROM GetAllChemistryWithTags");
                sb.AppendLine("ORDER BY NAME");

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
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
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SELECT ChemicalNameID, Name, Namespace, Tag");
                sb.AppendLine("FROM ChemicalNames");
                sb.AppendLine("WHERE ChemistryID = @id");
                sb.AppendLine("ORDER BY Name");

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@id", DbType.Int64).Value = id;
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
                return null;
            }
        }
    }
}