// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.TextFormatting;
using System.Xml.Linq;

namespace Chem4Word.ACME.Drawing
{
    public class BlockTextSource : TextSource
    {
        private string _colour;
        private List<LabelTextSourceRun> _runs = new List<LabelTextSourceRun>();
        public string Text { get; private set; }
        public int MaxLineLength { get; private set; }

        public BlockTextSource(string blockText, string colour)
        {
            MaxLineLength = 0;
            int currentLineLength = 0;
            Text = "";
            XDocument flowDoc = XDocument.Parse(blockText);
            _colour = colour;

            List<XNode> nodeList = flowDoc.DescendantNodes().ToList();
            foreach (var node in nodeList)
            {
                if (node is XElement xe)
                {
                    switch (xe.Name.LocalName)
                    {
                        case "LineBreak":
                            string text = xe.CreateReader().ReadInnerXml();
                            _runs.Add(new LabelTextSourceRun
                            {
                                Text = text,
                                IsEndParagraph = true
                            });

                            currentLineLength = 0;
                            break;

                        case "Run":
                            string text2 = xe.Value;
                            _runs.Add(new LabelTextSourceRun
                            {
                                Text = text2,
                                IsSubscript = xe.Attribute("BaselineAlignment")?.Value == "Subscript",
                                IsSuperscript = xe.Attribute("BaselineAlignment")?.Value == "Superscript"
                            });

                            currentLineLength += text2.Length;
                            if (currentLineLength > MaxLineLength)
                            {
                                MaxLineLength = currentLineLength;
                            }
                            break;
                    }
                }
                else if (node is XText xt)
                {
                    Text += xt.Value;
                }
            }
        }

        public double BlockTextSize { get; set; }
        public double BlockScriptSize { get; set; }

        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
        {
            throw new System.NotImplementedException();
        }

        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            throw new System.NotImplementedException();
        }

        public override TextRun GetTextRun(int textSourceCharacterIndex)
        {
            int pos = 0;
            foreach (var currentRun in _runs)
            {
                if (textSourceCharacterIndex < pos + currentRun.Length)
                {
                    if (currentRun.IsEndParagraph)
                    {
                        return new TextEndOfParagraph(1);
                    }

                    TextRunProperties props;
                    if (currentRun.IsSubscript)
                    {
                        props = new SubscriptTextRunProperties(_colour, BlockScriptSize);
                    }
                    else if (currentRun.IsSuperscript)
                    {
                        props = new SuperscriptTextRunProperties(_colour, BlockScriptSize);
                    }
                    else
                    {
                        props = new BlockTextRunProperties(_colour, BlockTextSize);
                    }

                    return new TextCharacters(
                        currentRun.Text,
                        textSourceCharacterIndex - pos,
                        currentRun.Length - (textSourceCharacterIndex - pos),
                        props);
                }
                pos += currentRun.Length;
            }
            // Return an end-of-paragraph if no more text source.
            return new TextEndOfParagraph(1);
        }
    }
}