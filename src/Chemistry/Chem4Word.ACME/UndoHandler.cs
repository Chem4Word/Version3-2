// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Chem4Word.ACME.Utils;
using IChem4Word.Contracts;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Manages undo/redo for a view model
    /// All actions to be recorded should be
    /// packaged as a pair of actions, for both undo and redo
    /// Every set of actions MUST be nested between
    /// BeginUndoBlock() and EndUndoBlock() calls
    ///
    /// </summary>
    public class UndoHandler
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private struct UndoRecord
        {
            public int Level;
            public string Description;
            public Action UndoAction;
            public Action RedoAction;

            public void Undo()
            {
                if (!IsBufferRecord())
                {
                    UndoAction();
                }
            }

            public void Redo()
            {
                if (!IsBufferRecord())
                {
                    RedoAction();
                }
            }

            public bool IsBufferRecord()
            {
                return Level == 0;
            }

            public override string ToString()
            {
                return $"Level {Level}, Description {Description}";
            }
        }

        //each block of transactions is bracketed by a buffer record at either end
        private readonly UndoRecord _startBracket, _endBracket;

        private EditViewModel _editViewModel;
        private IChem4WordTelemetry _telemetry;

        private Stack<UndoRecord> _undoStack;
        private Stack<UndoRecord> _redoStack;

        private int _transactionLevel = 0;

        public int TransactionLevel => _transactionLevel;
        public bool CanRedo => _redoStack.Any(rr => rr.Level != 0);

        public bool CanUndo => _undoStack.Any(ur => ur.Level != 0);
        public bool IsTopLevel => TransactionLevel == 1;

        public UndoHandler(EditViewModel vm, IChem4WordTelemetry telemetry)
        {
            _editViewModel = vm;
            _telemetry = telemetry;

            //set up the buffer record
            _startBracket = new UndoRecord
            {
                Description = "#start#",
                Level = 0,
                UndoAction = null,
                RedoAction = null
            };
            _endBracket = new UndoRecord
            {
                Description = "#end#",
                Level = 0,
                UndoAction = null,
                RedoAction = null
            };

            Initialize();
        }

        private void Initialize()
        {
            _undoStack = new Stack<UndoRecord>();
            _redoStack = new Stack<UndoRecord>();
        }

        public List<string> ReadUndoStack()
        {
            var result = new List<string>();
            foreach (var item in _undoStack)
            {
                result.Add($"{item.Level} - {item.Description}");
            }

            return result;
        }

        public List<string> ReadRedoStack()
        {
            var result = new List<string>();
            foreach (var item in _redoStack)
            {
                result.Add($"{item.Level} - {item.Description}");
            }

            return result;
        }

        private void WriteTelemetry(string source, string level, string message)
        {
            if (_telemetry != null)
            {
                _telemetry.Write(source, level, message);
            }
        }

        private void WriteTelemetryException(string source, Exception exception)
        {
            if (_telemetry != null)
            {
                _telemetry.Write(source, "Exception", exception.Message);
                _telemetry.Write(source, "Exception", exception.StackTrace);
            }
            else
            {
                RegistryHelper.StoreException(source, exception);
            }
        }

        public void BeginUndoBlock()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                //WriteTelemetry(module, "Debug", $"TransactionLevel: {_transactionLevel}");
                //push a buffer record onto the stack
                if (_transactionLevel == 0)
                {
                    _undoStack.Push(_startBracket);
                }
                _transactionLevel++;
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        public void RecordAction(Action undoAction, Action redoAction, [CallerMemberName] string desc = null)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                //performing a new action should clear the redo
                if (_redoStack.Any())
                {
                    _redoStack.Clear();
                }

                _undoStack.Push(new UndoRecord
                {
                    Level = _transactionLevel,
                    Description = desc,
                    UndoAction = undoAction,
                    RedoAction = redoAction
                });
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Ends a transaction block.  Transactions may be nested
        /// </summary>
        public void EndUndoBlock()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                //WriteTelemetry(module, "Debug", $"TransactionLevel: {_transactionLevel}");
                _transactionLevel--;

                if (_transactionLevel < 0)
                {
                    _telemetry.Write(module, "Exception", "Attempted to unwind empty undo stack.");
                    return;
                }

                //we've concluded a transaction block so terminated it
                if (_transactionLevel == 0)
                {
                    if (_undoStack.Peek().Equals(_startBracket))
                    {
                        _undoStack.Pop(); //no point in committing an empty block so just remove it
                    }
                    else
                    {
                        _undoStack.Push(_endBracket);
                    }
                }

                //tell the parent viewmodel the command status has changed
                _editViewModel.UndoCommand.RaiseCanExecChanged();
                _editViewModel.RedoCommand.RaiseCanExecChanged();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }


        public void Undo()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                UndoActions();
                _editViewModel.UndoCommand.RaiseCanExecChanged();
                _editViewModel.RedoCommand.RaiseCanExecChanged();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void UndoActions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            //the very first record on the undo stack should be a buffer record
            var br = _undoStack.Pop();
            WriteTelemetry(module, "Debug", $"{br.Level} - {br.Description}");

            if (!br.Equals(_endBracket))
            {
                Debugger.Break();
                throw new InvalidDataException("Undo stack is missing start bracket record");
            }
            _redoStack.Push(br);

            while (true)
            {
                br = _undoStack.Pop();
                WriteTelemetry(module, "Debug", $"{br.Level} - {br.Description}");
                _redoStack.Push(br);
                if (br.Equals(_startBracket))
                {
                    break;
                }
                br.Undo();
            }

#if DEBUG
            var integrity = _editViewModel.Model.CheckIntegrity();
            if (integrity.Count > 0)
            {
                WriteTelemetry(module, "Integrity", string.Join(Environment.NewLine, integrity));
            }
#endif
        }

        public void Redo()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                RedoActions();
                _editViewModel.UndoCommand.RaiseCanExecChanged();
                _editViewModel.RedoCommand.RaiseCanExecChanged();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        private void RedoActions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            //the very first record on the redo stack should be a buffer record
            var br = _redoStack.Pop();
            WriteTelemetry(module, "Debug", $"{br.Level} - {br.Description}");

            if (!br.Equals(_startBracket))
            {
                Debugger.Break();
                throw new InvalidDataException("Redo stack is missing end bracket record");
            }
            _undoStack.Push(br);

            while (true)
            {
                br = _redoStack.Pop();
                WriteTelemetry(module, "Debug", $"{br.Level} - {br.Description}");

                _undoStack.Push(br);
                if (br.Equals(_endBracket))
                {
                    break;
                }
                br.Redo();
            }

#if DEBUG
            var integrity = _editViewModel.Model.CheckIntegrity();
            if (integrity.Count > 0)
            {
                WriteTelemetry(module, "Integrity", string.Join(Environment.NewLine, integrity));
            }
#endif
        }
    }
}