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
using Newtonsoft.Json;

namespace Chem4Word.Libraries.Database
{
    public class Library
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private readonly TelemetryWriter _telemetry;
        private readonly LibraryOptions _options;

        private readonly SdFileConverter _sdFileConverter;
        private readonly CMLConverter _cmlConverter;

        private readonly List<Patch> _patches;

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

            // Read patches from resource
            var resource = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Patches.json");
            _patches = JsonConvert.DeserializeObject<List<Patch>>(resource);

            Patch(_patches.Max(p => p.Version));
        }

        private SQLiteConnection LibraryConnection()
        {
            string path = Path.Combine(_options.ProgramDataPath, Constants.LibraryFileName);
            // Source https://www.connectionstrings.com/sqlite/
            var conn = new SQLiteConnection($"Data Source={path};Synchronous=Full");

            return conn.OpenAndReturn();
        }

        private void Patch(Version targetVersion)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                using (SQLiteConnection conn = LibraryConnection())
                {
                    bool patchTableExists = false;
                    bool isLegacy = false;

                    var currentVersion = Version.Parse("0.0.0");

                    using (SQLiteDataReader tables = GetListOfTablesAndViews(conn))
                    {
                        if (tables != null)
                        {
                            while (tables.Read())
                            {
                                if (tables["Name"] is string name)
                                {
                                    var type = tables["Type"] as string;
                                    Debug.WriteLine($"Found {type} '{name}'");

                                    if (name.Equals("Patches"))
                                    {
                                        patchTableExists = true;
                                    }

                                    if (name.Equals("Gallery"))
                                    {
                                        isLegacy = true;
                                    }
                                }
                            }
                        }
                    }

                    if (patchTableExists)
                    {
                        // Read current patch level
                        using (SQLiteDataReader patches = GetListOfPatches(conn))
                        {
                            if (patches != null)
                            {
                                while (patches.Read())
                                {
                                    if (patches["Version"] is string version)
                                    {
                                        var applied = patches["Applied"] as string;
                                        Debug.WriteLine($"Patch {version} was applied on {applied}");

                                        var thisVersion = Version.Parse(version);
                                        if (thisVersion > currentVersion)
                                        {
                                            currentVersion = thisVersion;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (isLegacy && currentVersion < targetVersion)
                    {
                        // Backup before patching
                        var database = Path.Combine(_options.ProgramDataPath, Constants.LibraryFileName);
                        var backup = Path.Combine(_options.ProgramDataPath, $"{SafeDate.ToIsoFilePrefix(DateTime.Now)} {Constants.LibraryFileName}");
                        File.Copy(database, backup);

                        if (!ApplyPatches(conn, currentVersion))
                        {
                            // If patching fails, revert to previous version
                            File.Delete(database);
                            File.Copy(backup, database);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception {ex.Message} in {module}");
            }
        }

        private SQLiteDataReader GetListOfPatches(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            SQLiteDataReader result = null;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Version, Applied");
                sb.AppendLine("FROM Patches");

                var command = new SQLiteCommand(sb.ToString(), conn);
                result = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception {ex.Message} in {module}");
            }

            return result;
        }

        private SQLiteDataReader GetListOfTablesAndViews(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            SQLiteDataReader result = null;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT t.Name, t.Type");
                sb.AppendLine("FROM sqlite_master t");
                sb.AppendLine("WHERE t.Type IN ('table','view')");

                var command = new SQLiteCommand(sb.ToString(), conn);
                result = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception {ex.Message} in {module}");
            }

            return result;
        }

        private bool ApplyPatches(SQLiteConnection conn, Version currentVersion)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            bool result = true;

            try
            {
                foreach (Patch patch in _patches)
                {
                    if (patch.Version > currentVersion)
                    {
                        foreach (string script in patch.Scripts)
                        {
                            Debug.WriteLine($"Applying patch '{script}'");
                            var command = new SQLiteCommand(script, conn);
                            command.ExecuteNonQuery();
                        }
                    }
                    AddPatchRecord(conn, patch.Version);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception {ex.Message} in {module}");
                result = false;
            }

            return result;
        }

        private void AddPatchRecord(SQLiteConnection conn, Version version)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                var versionString = version.ToString(3);

                var sb = new StringBuilder();

                sb.AppendLine("INSERT INTO Patches");
                sb.AppendLine(" (Version, Applied)");
                sb.AppendLine("VALUES");
                sb.AppendLine(" (@version, @applied)");

                var command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@version", DbType.String, versionString.Length).Value = versionString;
                var applied = SafeDate.ToShortDate(DateTime.Today);
                command.Parameters.Add("@applied", DbType.String, applied.Length).Value = applied;

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// This is called by LoadNamesFromLibrary in Add-In which happens on right click
        /// </summary>
        /// <returns></returns>
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
                        if (names != null)
                        {
                            while (names.Read())
                            {
                                var name = names["Name"] as string;
                                // Exclude any names less than three characters
                                if (!string.IsNullOrEmpty(name) && name.Length > 3)
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

        private SQLiteDataReader GetAllNames(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            SQLiteDataReader result;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT DISTINCT Name, ChemistryId");
                sb.AppendLine("FROM ChemicalNames");
                sb.AppendLine("WHERE NOT (Namespace = 'chem4word' AND Tag = 'cev_freq')");
                sb.AppendLine(" AND NOT (Namespace = 'pubchem' AND Tag = 'Id')");
                sb.AppendLine(" AND NOT (Name = 'chemical compound')");
                sb.AppendLine("UNION");
                sb.AppendLine("SELECT DISTINCT Name, Id");
                sb.AppendLine("FROM Gallery");

                var command = new SQLiteCommand(sb.ToString(), conn);
                result = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                result = null;
            }

            return result;
        }

        /// <summary>
        /// This is called by ...
        /// </summary>
        public void DeleteAllChemistry()
        {
            using (SQLiteConnection conn = LibraryConnection())
            {
                DeleteAllChemistry(conn);
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

        /// <summary>
        /// This is called by ...
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// This is called by ...
        /// </summary>
        /// <param name="cmlFile"></param>
        /// <param name="transaction"></param>
        /// <param name="calculateProperties"></param>
        /// <returns></returns>
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

        /// <summary>
        /// This is called by ...
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="xml"></param>
        /// <param name="formula"></param>
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

        /// <summary>
        /// This is called by ...
        /// </summary>
        /// <param name="chemistryId"></param>
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

        /// <summary>
        /// This is called by Library Task Pane Import Button
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetChemistryById(long id)
        {
            string result = null;

            using (SQLiteConnection conn = LibraryConnection())
            {
                using (SQLiteDataReader chemistry = GetChemistryById(conn, id))
                {
                    if (chemistry != null)
                    {
                        while (chemistry.Read())
                        {
                            result = CmlFromBytes((byte[])chemistry["Chemistry"]);
                        }
                    }
                }
            }

            return result;
        }

        private SQLiteDataReader GetChemistryById(SQLiteConnection conn, long id)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            SQLiteDataReader result;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Id, Chemistry, Name, Formula");
                sb.AppendLine("FROM Gallery");
                sb.AppendLine("WHERE ID = @id");

                var command = new SQLiteCommand(sb.ToString(), conn);
                command.Parameters.Add("@id", DbType.Int64).Value = id;
                result = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                result = null;
            }

            return result;
        }

        /// <summary>
        /// This is called by LibraryVieModel.AddNewChemistry()
        /// </summary>
        /// <param name="model"></param>
        /// <param name="chemistryName"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public long AddChemistry(Model model, string chemistryName, string formula)
        {
            long result;
            using (SQLiteConnection conn = LibraryConnection())
            {
                result = AddChemistry(conn, model, chemistryName, formula);
            }

            return result;
        }

        /// <summary>
        /// This is called by ImportCml(), ImportCml()
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="model"></param>
        /// <param name="chemistryName"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
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
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
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
                using (SQLiteDataReader chemistry = GetAllChemistry(conn))
                {
                    if (chemistry != null)
                    {
                        while (chemistry.Read())
                        {
                            var id = (long)chemistry["ID"];
                            var dto = new ChemistryDTO
                            {
                                Id = id,
                                Cml = CmlFromBytes((byte[])chemistry["Chemistry"]),
                                Name = chemistry["name"] as string,
                                Formula = chemistry["formula"] as string,
                                Tags = FetchTags(id)
                            };

                            results.Add(dto);
                        }
                    }
                }
            }

            sw.Stop();
            _telemetry.Write(module, "Timing", $"Reading {results.Count} structures took {SafeDouble.AsString(sw.ElapsedMilliseconds)}ms");

            return results;
        }

        private SQLiteDataReader GetAllChemistry(SQLiteConnection conn)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            SQLiteDataReader result = null;

            try
            {
                var sb = new StringBuilder();

                sb.AppendLine("SELECT Id, Chemistry, Name, Formula");
                sb.AppendLine("FROM Gallery");
                sb.AppendLine("ORDER BY Name");

                var command = new SQLiteCommand(sb.ToString(), conn);
                result = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(_telemetry, _options.ParentTopLeft, module, ex))
                {
                    form.ShowDialog();
                }

                result = null;
            }

            return result;
        }

        private readonly List<string> _animals = new List<string>
                                                 {
                                                     "Rat", "Ox", "Tiger", "Rabbit", "Dragon", "Snake", "Horse", "Goat", "Monkey", "Rooster", "Dog", "Pig", "Bat", "Cat"
                                                 };

        private readonly Random _random = new Random(Environment.TickCount);

        private List<UserTagDTO> FetchTags(long id)
        {
            var result = new List<UserTagDTO>();
            int count = _random.Next((int)(id % 8));

            var shuffled = _animals.OrderBy(i => Guid.NewGuid()).ToList();

            while (count > 0)
            {
                int index = _random.Next(shuffled.Count);
                var name = shuffled[index];
                shuffled.RemoveAt(index);

                var tag = new UserTagDTO
                          {
                              Text = name
                          };
                result.Add(tag);

                count--;
            }

            return result;
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
    }
}