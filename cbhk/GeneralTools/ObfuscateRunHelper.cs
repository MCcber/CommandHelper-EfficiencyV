using cbhk.CustomControls;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;

namespace cbhk.GeneralTools
{
    public class ObfuscateRunHelper
    {
        /// <summary>
        /// 执行混淆文字效果
        /// </summary>
        public static void Run(ref RichTextBox richTextBox)
        {
            RichRun startRun = richTextBox.Selection.Start.Parent as RichRun;
            RichRun endRun = richTextBox.Selection.End.Parent as RichRun;
            int startIndex;
            int endIndex;
            RichParagraph startParagraph = richTextBox.Selection.Start.Paragraph as RichParagraph;
            RichParagraph endParagraph = richTextBox.Selection.End.Paragraph as RichParagraph;
            List<RichRun> startRunList = startParagraph.Inlines.Cast<RichRun>().ToList();
            List<RichRun> endRunList = endParagraph.Inlines.Cast<RichRun>().ToList();
            bool NeedObfuscate = false;
            if (Equals(richTextBox.Selection.Start.Paragraph, richTextBox.Selection.End.Paragraph))
            {
                startIndex = startRunList.IndexOf(startRun);
                endIndex = startRunList.IndexOf(endRun);
                for (int i = startIndex; i <= endIndex; i++)
                {
                    NeedObfuscate |= !startRunList[i].IsObfuscated;
                    if (NeedObfuscate)
                    {
                        break;
                    }
                }
                if(NeedObfuscate)
                {
                    if(!startRun.Equals(endRun))
                    {
                        if (!richTextBox.Selection.Start.Equals(startRun.ContentStart))
                        {
                            TextRange textRange = new(startRun.ContentStart, richTextBox.Selection.Start);
                            if (textRange.Text.Trim().Length > 0)
                            {
                                startParagraph.Inlines.InsertBefore(startRun, new RichRun() { Text = textRange.Text });
                                startRun.Text = new TextRange(richTextBox.Selection.Start, startRun.ContentEnd).Text;
                            }
                        }
                        startRun.UID = startRun.Text;
                        startRun.IsObfuscated = true;
                        if (!richTextBox.Selection.End.Equals(endRun.ContentEnd))
                        {
                            TextRange textRange = new(endRun.ContentEnd, richTextBox.Selection.End);
                            if (textRange.Text.Trim().Length > 0)
                            {
                                endParagraph.Inlines.InsertAfter(endRun, new RichRun() { Text = textRange.Text });
                                endRun.Text = new TextRange(richTextBox.Selection.End, endRun.ContentStart).Text;
                            }
                        }
                        endRun.UID = endRun.Text;
                        endRun.IsObfuscated = true;
                    }
                    else
                    {
                        TextRange startPartTextRange = null;
                        TextRange endPartTextRange = null;
                        if (!richTextBox.Selection.Start.Equals(startRun.ContentStart))
                            startPartTextRange = new(startRun.ContentStart, richTextBox.Selection.Start);
                        if (!richTextBox.Selection.End.Equals(endRun.ContentEnd))
                            endPartTextRange = new(startRun.ContentEnd, richTextBox.Selection.End);
                        int startRunStartIndex = 0;
                        int startRunEndIndex = 0;
                        if (startPartTextRange is not null && startPartTextRange.Text.Trim().Length > 0)
                        {
                            startParagraph.Inlines.InsertBefore(startRun, new RichRun() { Text = startPartTextRange.Text });
                            startRunStartIndex = startPartTextRange.Text.Length;
                        }
                        if (endPartTextRange is not null && endPartTextRange.Text.Trim().Length > 0)
                        {
                            endParagraph.Inlines.InsertAfter(startRun, new RichRun() { Text = endPartTextRange.Text });
                            startRunEndIndex = endPartTextRange.Text.Length;
                        }
                        if (startRunEndIndex >= startRunStartIndex)
                            startRun.Text = startRun.Text[startRunStartIndex..^startRunEndIndex];
                    }
                    startRun.UID = startRun.Text;
                    startRun.IsObfuscated = true;

                    for (int i = startIndex + 1; i < endIndex; i++)
                    {
                        startRunList[i].UID = startRunList[i].Text;
                        startRunList[i].IsObfuscated = true;
                    }
                }
                else
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        startRunList[i].IsObfuscated = false;
                        startRunList[i].Text = startRunList[i].UID;
                    }
                }
            }
            else
            {
                startIndex = startRunList.IndexOf(startRun);
                endIndex = endRunList.IndexOf(endRun);
                for (int i = startIndex; i < startRunList.Count; i++)
                {
                    NeedObfuscate |= !startRunList[i].IsObfuscated;
                    if (NeedObfuscate)
                    {
                        break;
                    }
                }
                if(!NeedObfuscate)
                {
                    for (int i = 0; i <= endIndex; i++)
                    {
                        NeedObfuscate |= !endRunList[i].IsObfuscated;
                        if (NeedObfuscate)
                        {
                            break;
                        }
                    }
                }
                if (NeedObfuscate)
                {
                    if (!richTextBox.Selection.Start.Equals(startRun.ContentStart))
                    {
                        TextRange textRange = new(startRun.ContentStart, richTextBox.Selection.Start);
                        if (textRange.Text.Trim().Length > 0)
                        {
                            startRun.Text = new TextRange(richTextBox.Selection.Start, startRun.ContentEnd).Text;
                            startParagraph.Inlines.InsertBefore(startRun, new RichRun() { Text = textRange.Text });
                        }
                    }
                    startRun.UID = startRun.Text;
                    startRun.IsObfuscated = true;
                    if (!richTextBox.Selection.End.Equals(endRun.ContentEnd))
                    {
                        TextRange textRange = new(endRun.ContentEnd, richTextBox.Selection.End);
                        if (textRange.Text.Trim().Length > 0)
                        {
                            endRun.Text = new TextRange(richTextBox.Selection.End, endRun.ContentStart).Text;
                            endParagraph.Inlines.InsertAfter(endRun, new RichRun() { Text = textRange.Text });
                        }
                    }
                    endRun.UID = endRun.UID;
                    endRun.IsObfuscated = true;
                    for (int i = startIndex + 1; i < endIndex; i++)
                    {
                        startRunList[i].UID = startRunList[i].Text;
                        startRunList[i].IsObfuscated = true;
                    }
                }
                else
                {
                    for (int i = startIndex; i < startRunList.Count; i++)
                    {
                        startRunList[i].Text = startRunList[i].UID;
                        startRunList[i].IsObfuscated = false;
                    }
                    for (int i = 0; i <= endIndex; i++)
                    {
                        endRunList[i].Text = endRunList[i].UID;
                        endRunList[i].IsObfuscated = false;
                    }
                }
            }
        }
    }
}