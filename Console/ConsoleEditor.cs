/// ConsoleEditor: multi-line console editor with:
/// - visual wrapping according to Console.WindowWidth
/// - cursor movement (visual and logical)
/// - Insert/Overwrite mode (Insert)
/// - Backspace/Delete, Home/End, Enter (new logical line)
/// - PageUp/PageDown (visual-page navigation)
/// - Undo (Ctrl+Z) / Redo (Ctrl+Y)
/// - History of entered blocks (added on finish) and retrieval by Up/Down when at document top/bottom
/// - Finishing input by Ctrl+C (CancelKeyPress handled; original handlers are temporarily detached and restored)
/// Designed for compatibility with older .NET (avoid LINQ, use basic constructs).

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

/// <summary>
/// Represent multi-line console editor.
/// </summary>
public static class ConsoleEditor
{
    // Public history storage for blocks - persisted for the lifetime of the process.
    // User can pre-populate or read it after.
    public static List<string> History = new List<string>();

    // Maximum undo/redo stack size
    private const int MaxUndo = 100;

    // Internal representation for visual segment
    private class VisualSegment
    {
        public int Left;          // starting column (visual)
        public string Text;       // text of segment (substring of logical line)
        public int LogicalLine;   // which logical line it belongs to
        public int StartPos;      // start position (index) inside logical line
        public VisualSegment(int left, string text, int logicalLine, int startPos)
        {
            Left = left;
            Text = text;
            LogicalLine = logicalLine;
            StartPos = startPos;
            Text = text ?? string.Empty;
        }
    }

    // State snapshot for undo/redo
    private class EditorState
    {
        public List<string> Lines;
        public int CurLine;
        public int CurCol;
        public bool InsertMode;
        public EditorState(List<string> lines, int curLine, int curCol, bool insertMode)
        {
            Lines = new List<string>(lines.Count);
            for (int i = 0; i < lines.Count; i++) Lines.Add(lines[i] ?? string.Empty);
            CurLine = curLine;
            CurCol = curCol;
            InsertMode = insertMode;
        }
    }

    /// <summary>
    /// ReadAllLines: start interactive multi-line editor in console.
    /// Finish by pressing Ctrl+C (the method cancels process termination and returns the text).
    /// </summary>
    public static string ReadAllLines()
    {
        // Save starting cursor position
        int startLeft = Console.CursorLeft;
        int startTop = Console.CursorTop;

        // logical lines
        List<string> lines = new List<string>();
        lines.Add(string.Empty);
        int curLine = 0;
        int curCol = 0;

        // Insert mode default: true = insert, false = overwrite
        bool insertMode = true;

        // Undo/Redo stacks
        List<EditorState> undoStack = new List<EditorState>();
        List<EditorState> redoStack = new List<EditorState>();

        // previous visual render segments (for clearing)
        List<VisualSegment> prevRender = new List<VisualSegment>();

        // preferred visual column to attempt to preserve when moving vertically
        int preferredVisualCol = -1;

        // history navigation index (History is static). value equals History.Count means "new"
        int historyIndex = History.Count;

        // finished flag
        bool finished = false;

        // Our cancel handler and reflection helper for saving/restoring other handlers
        ConsoleCancelEventHandler ourHandler = null;
        CancelHandlersReflectionHelper.CancelHandlersSnapshot savedSnapshot = null;

        // helper to push current snapshot to undo stack
        Action pushUndo = delegate()
        {
            try
            {
                // push current state
                EditorState st = new EditorState(lines, curLine, curCol, insertMode);
                undoStack.Add(st);
                // limit size
                if (undoStack.Count > MaxUndo) undoStack.RemoveAt(0);
                // any new change invalidates redo stack
                redoStack.Clear();
            }
            catch
            {
                // ignore snapshot failures
            }
        };

        // Attach our CancelKeyPress handler and detach others (via reflection)
        ourHandler = new ConsoleCancelEventHandler(delegate(object sender, ConsoleCancelEventArgs e)
        {
            // Cancel termination and signal finish
            e.Cancel = true;
            finished = true;
        });

        try
        {
            // Save existing handlers and detach them (keeping only ours)
            savedSnapshot = CancelHandlersReflectionHelper.DetachAllExcept(ourHandler);

            // Register our handler
            Console.CancelKeyPress += ourHandler;

            // Initial render
            RedrawAll();

            // main loop
            while (!finished)
            {
                ConsoleKeyInfo keyInfo;
                try
                {
                    keyInfo = Console.ReadKey(true);
                }
                catch (InvalidOperationException)
                {
                    // console not available - abort
                    break;
                }

                // Defensive: treat Ctrl+C from ReadKey too
                if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0 && keyInfo.Key == ConsoleKey.C)
                {
                    finished = true;
                    break;
                }

                bool contentChanged = false;
                bool cursorMoved = false;
                bool structuralChange = false; // e.g., Enter/new line/merge lines

                // For left/right/home/end and character edits we reset preferredVisualCol,
                // for vertical moves we use/retain it.
                switch (keyInfo.Key)
                {
                    case ConsoleKey.LeftArrow:
                        if (curCol > 0)
                        {
                            curCol--;
                        }
                        else if (curLine > 0)
                        {
                            curLine--;
                            curCol = lines[curLine].Length;
                        }
                        preferredVisualCol = -1;
                        cursorMoved = true;
                        break;

                    case ConsoleKey.RightArrow:
                        if (curCol < lines[curLine].Length)
                        {
                            curCol++;
                        }
                        else if (curLine < lines.Count - 1)
                        {
                            curLine++;
                            curCol = 0;
                        }
                        preferredVisualCol = -1;
                        cursorMoved = true;
                        break;

                    case ConsoleKey.Home:
                        curCol = 0;
                        preferredVisualCol = -1;
                        cursorMoved = true;
                        break;

                    case ConsoleKey.End:
                        curCol = lines[curLine].Length;
                        preferredVisualCol = -1;
                        cursorMoved = true;
                        break;

                    case ConsoleKey.UpArrow:
                        {
                            // Build visual map and find current visual index
                            List<VisualSegment> segs = BuildVisualMap(lines, startLeft, Console.WindowWidth);
                            int visualIndex = VisualIndexOf(segs, curLine, curCol);
                            if (visualIndex <= 0)
                            {
                                // at top visual row -> maybe history previous
                                if (History != null && History.Count > 0)
                                {
                                    if (historyIndex > 0) historyIndex--;
                                    if (historyIndex < 0) historyIndex = 0;
                                    // load history entry
                                    if (historyIndex >= 0 && historyIndex < History.Count)
                                    {
                                        pushUndo();
                                        LoadBlockFromHistory(History[historyIndex], lines, out curLine, out curCol);
                                        contentChanged = true;
                                    }
                                }
                                else
                                {
                                    // nothing to do
                                }
                            }
                            else
                            {
                                // move to previous visual segment
                                if (preferredVisualCol < 0)
                                {
                                    int curLeft, curTop;
                                    GetCursorVisualPosition(lines, curLine, curCol, startLeft, startTop, Console.WindowWidth, out curLeft, out curTop);
                                    preferredVisualCol = curLeft;
                                }
                                int targetVisual = visualIndex - 1;
                                MoveCursorToVisualIndex(segs, targetVisual, ref curLine, ref curCol, preferredVisualCol);
                                cursorMoved = true;
                            }
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        {
                            List<VisualSegment> segs = BuildVisualMap(lines, startLeft, Console.WindowWidth);
                            int visualIndex = VisualIndexOf(segs, curLine, curCol);
                            if (visualIndex >= segs.Count - 1)
                            {
                                // at bottom -> maybe next history
                                if (History != null && History.Count > 0)
                                {
                                    if (historyIndex < History.Count) historyIndex++;
                                    if (historyIndex > History.Count) historyIndex = History.Count;
                                    if (historyIndex >= 0 && historyIndex < History.Count)
                                    {
                                        pushUndo();
                                        LoadBlockFromHistory(History[historyIndex], lines, out curLine, out curCol);
                                        contentChanged = true;
                                    }
                                    else if (historyIndex == History.Count)
                                    {
                                        // "new" empty block - clear to empty
                                        pushUndo();
                                        lines.Clear();
                                        lines.Add(string.Empty);
                                        curLine = 0; curCol = 0;
                                        contentChanged = true;
                                    }
                                }
                            }
                            else
                            {
                                if (preferredVisualCol < 0)
                                {
                                    int curLeft, curTop;
                                    GetCursorVisualPosition(lines, curLine, curCol, startLeft, startTop, Console.WindowWidth, out curLeft, out curTop);
                                    preferredVisualCol = curLeft;
                                }
                                int targetVisual = visualIndex + 1;
                                MoveCursorToVisualIndex(segs, targetVisual, ref curLine, ref curCol, preferredVisualCol);
                                cursorMoved = true;
                            }
                        }
                        break;

                    case ConsoleKey.PageUp:
                        {
                            List<VisualSegment> segs = BuildVisualMap(lines, startLeft, Console.WindowWidth);
                            int visualIndex = VisualIndexOf(segs, curLine, curCol);
                            int delta = Console.WindowHeight - 1;
                            int target = visualIndex - delta;
                            if (target < 0) target = 0;
                            if (preferredVisualCol < 0)
                            {
                                int curLeft, curTop;
                                GetCursorVisualPosition(lines, curLine, curCol, startLeft, startTop, Console.WindowWidth, out curLeft, out curTop);
                                preferredVisualCol = curLeft;
                            }
                            MoveCursorToVisualIndex(segs, target, ref curLine, ref curCol, preferredVisualCol);
                            cursorMoved = true;
                        }
                        break;

                    case ConsoleKey.PageDown:
                        {
                            List<VisualSegment> segs = BuildVisualMap(lines, startLeft, Console.WindowWidth);
                            int visualIndex = VisualIndexOf(segs, curLine, curCol);
                            int delta = Console.WindowHeight - 1;
                            int target = visualIndex + delta;
                            if (target > segs.Count - 1) target = segs.Count - 1;
                            if (preferredVisualCol < 0)
                            {
                                int curLeft, curTop;
                                GetCursorVisualPosition(lines, curLine, curCol, startLeft, startTop, Console.WindowWidth, out curLeft, out curTop);
                                preferredVisualCol = curLeft;
                            }
                            MoveCursorToVisualIndex(segs, target, ref curLine, ref curCol, preferredVisualCol);
                            cursorMoved = true;
                        }
                        break;

                    case ConsoleKey.Insert:
                        // toggle insert/overwrite
                        insertMode = !insertMode;
                        cursorMoved = false;
                        break;

                    case ConsoleKey.Backspace:
                        if (curCol > 0)
                        {
                            pushUndo();
                            string s = lines[curLine];
                            lines[curLine] = s.Substring(0, curCol - 1) + s.Substring(curCol);
                            curCol--;
                            contentChanged = true;
                        }
                        else if (curLine > 0)
                        {
                            pushUndo();
                            int prevLen = lines[curLine - 1].Length;
                            lines[curLine - 1] = lines[curLine - 1] + lines[curLine];
                            lines.RemoveAt(curLine);
                            curLine--;
                            curCol = prevLen;
                            contentChanged = true;
                            structuralChange = true;
                        }
                        preferredVisualCol = -1;
                        break;

                    case ConsoleKey.Delete:
                        if (curCol < lines[curLine].Length)
                        {
                            pushUndo();
                            string s = lines[curLine];
                            lines[curLine] = s.Substring(0, curCol) + s.Substring(curCol + 1);
                            contentChanged = true;
                        }
                        else if (curLine < lines.Count - 1)
                        {
                            pushUndo();
                            lines[curLine] = lines[curLine] + lines[curLine + 1];
                            lines.RemoveAt(curLine + 1);
                            contentChanged = true;
                            structuralChange = true;
                        }
                        preferredVisualCol = -1;
                        break;

                    case ConsoleKey.Enter:
                        pushUndo();
                        {
                            string s = lines[curLine];
                            string leftPart = s.Substring(0, curCol);
                            string rightPart = s.Substring(curCol);
                            lines[curLine] = leftPart;
                            lines.Insert(curLine + 1, rightPart);
                            curLine++;
                            curCol = 0;
                            contentChanged = true;
                            structuralChange = true;
                            preferredVisualCol = -1;
                        }
                        break;

                    case ConsoleKey.Z:
                        // Ctrl+Z -> Undo
                        if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                        {
                            if (undoStack.Count > 0)
                            {
                                try
                                {
                                    // push current to redo
                                    EditorState cur = new EditorState(lines, curLine, curCol, insertMode);
                                    redoStack.Add(cur);

                                    // pop last undo
                                    EditorState last = undoStack[undoStack.Count - 1];
                                    undoStack.RemoveAt(undoStack.Count - 1);

                                    // restore
                                    lines = CloneLines(last.Lines);
                                    curLine = last.CurLine;
                                    curCol = last.CurCol;
                                    insertMode = last.InsertMode;

                                    contentChanged = true;
                                    // keep preferredVisualCol as-is to try to preserve horizontal
                                }
                                catch
                                {
                                }
                            }
                        }
                        break;

                    case ConsoleKey.Y:
                        // Ctrl+Y -> Redo
                        if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                        {
                            if (redoStack.Count > 0)
                            {
                                try
                                {
                                    EditorState cur = new EditorState(lines, curLine, curCol, insertMode);
                                    undoStack.Add(cur);

                                    EditorState next = redoStack[redoStack.Count - 1];
                                    redoStack.RemoveAt(redoStack.Count - 1);

                                    lines = CloneLines(next.Lines);
                                    curLine = next.CurLine;
                                    curCol = next.CurCol;
                                    insertMode = next.InsertMode;

                                    contentChanged = true;
                                }
                                catch
                                {
                                }
                            }
                        }
                        break;

                    default:
                        {
                            char ch = keyInfo.KeyChar;
                            if (ch != '\0' && !char.IsControl(ch))
                            {
                                pushUndo();
                                string s = lines[curLine];
                                if (insertMode || curCol >= s.Length)
                                {
                                    // insert
                                    lines[curLine] = s.Substring(0, curCol) + ch + s.Substring(curCol);
                                }
                                else
                                {
                                    // overwrite
                                    StringBuilder sb = new StringBuilder(s);
                                    sb[curCol] = ch;
                                    lines[curLine] = sb.ToString();
                                }
                                curCol++;
                                contentChanged = true;
                                preferredVisualCol = -1;
                            }
                        }
                        break;
                } // switch

                if (contentChanged || cursorMoved || structuralChange)
                {
                    // When content changed, reset history index (editing manual content)
                    historyIndex = History.Count;
                    RedrawAll();
                }
            } // while

            // when finished, push final block to history if non-empty
            string finalText = JoinLines(lines);
            if (finalText.Length > 0)
            {
                // avoid consecutive duplicates
                if (History.Count == 0 || History[History.Count - 1] != finalText)
                {
                    History.Add(finalText);
                }
            }

            // ensure cursor placed after editor
            MoveCursorAfterEditor();
            return finalText;
        }
        finally
        {
            // restore CancelKeyPress handlers reliably
            try
            {
                if (ourHandler != null)
                {
                    Console.CancelKeyPress -= ourHandler;
                }
            }
            catch
            {
            }
            try
            {
                if (savedSnapshot != null)
                {
                    CancelHandlersReflectionHelper.RestoreSnapshot(savedSnapshot);
                }
            }
            catch
            {
            }
        }

        // --- local helper methods below ---

        // redraw wrapper
        void RedrawAll()
        {
            try
            {
                Redraw(lines, startLeft, ref startTop, curLine, curCol, insertMode, ref prevRender);
            }
            catch
            {
                // best-effort redraw
            }
        }

        // move cursor to visual index preserving preferredVisualCol
        void MoveCursorToVisualIndex(List<VisualSegment> segs, int targetVisualIndex, ref int outLine, ref int outCol, int prefVisual)
        {
            if (segs == null || segs.Count == 0) return;
            if (targetVisualIndex < 0) targetVisualIndex = 0;
            if (targetVisualIndex >= segs.Count) targetVisualIndex = segs.Count - 1;
            VisualSegment vs = segs[targetVisualIndex];
            int leftBase = vs.Left;
            int offsetInSeg = prefVisual - leftBase;
            if (offsetInSeg < 0) offsetInSeg = 0;
            if (offsetInSeg > vs.Text.Length) offsetInSeg = vs.Text.Length;
            outLine = vs.LogicalLine;
            outCol = vs.StartPos + offsetInSeg;
            if (outCol < 0) outCol = 0;
            if (outCol > lines[outLine].Length) outCol = lines[outLine].Length;
        }

        // compute visual index containing (line,col)
        int VisualIndexOf(List<VisualSegment> segs, int line, int col)
        {
            if (segs == null || segs.Count == 0) return 0;
            for (int i = 0; i < segs.Count; i++)
            {
                VisualSegment s = segs[i];
                if (s.LogicalLine == line)
                {
                    // segment covers start..start+len
                    if (col >= s.StartPos && col <= s.StartPos + s.Text.Length)
                    {
                        return i;
                    }
                }
            }
            // if not found, approximate: find last segment of previous lines
            for (int i = segs.Count - 1; i >= 0; i--)
            {
                if (segs[i].LogicalLine <= line)
                    return i;
            }
            return 0;
        }

        // load block from history string into lines and place caret at end
        void LoadBlockFromHistory(string block, List<string> targetLines, out int outLine, out int outCol)
        {
            targetLines.Clear();
            string[] parts = SplitByEnvironmentNewLine(block);
            for (int i = 0; i < parts.Length; i++) targetLines.Add(parts[i]);
            if (targetLines.Count == 0) targetLines.Add(string.Empty);
            outLine = targetLines.Count - 1;
            outCol = targetLines[outLine].Length;
        }

        // split string by Environment.NewLine into array (no LINQ)
        string[] SplitByEnvironmentNewLine(string text)
        {
            string nl = Environment.NewLine;
            List<string> parts = new List<string>();
            if (string.IsNullOrEmpty(nl))
            {
                parts.Add(text ?? string.Empty);
                return parts.ToArray();
            }

            int pos = 0;
            int idx;
            while ((idx = IndexOf(text, nl, pos)) >= 0)
            {
                parts.Add(text.Substring(pos, idx - pos));
                pos = idx + nl.Length;
            }
            if (pos <= (text ?? string.Empty).Length)
            {
                parts.Add((text ?? string.Empty).Substring(pos));
            }
            return parts.ToArray();
        }

        // safe IndexOf for substring
        int IndexOf(string src, string pattern, int start)
        {
            if (src == null) return -1;
            return src.IndexOf(pattern, start, StringComparison.Ordinal);
        }

        // clone lines list
        List<string> CloneLines(List<string> src)
        {
            List<string> dst = new List<string>(src.Count);
            for (int i = 0; i < src.Count; i++) dst.Add(src[i] ?? string.Empty);
            return dst;
        }

        // compute final cursor placement after editor (one empty line)
        void MoveCursorAfterEditor()
        {
            try
            {
                List<VisualSegment> finalSegs = BuildVisualMap(lines, startLeft, Console.WindowWidth);
                int usedRows = finalSegs.Count;
                int finalRow = startTop + usedRows;
                if (finalRow >= Console.BufferHeight)
                {
                    for (int i = 0; i <= finalRow - Console.BufferHeight; i++) Console.WriteLine();
                    finalRow = Console.CursorTop;
                }
                Console.SetCursorPosition(0, finalRow);
            }
            catch
            {
                Console.WriteLine();
            }
        }
    } // end ReadAllLines

    // ---------------------------
    // Rendering and mapping logic
    // ---------------------------

    // Build visual map: array of VisualSegment describing how logical lines are split visually.
    private static List<VisualSegment> BuildVisualMap(List<string> lines, int startLeft, int windowWidth)
    {
        List<VisualSegment> result = new List<VisualSegment>();
        if (windowWidth <= 0) windowWidth = 80;
        int firstWidth = windowWidth - startLeft;
        if (firstWidth <= 0) firstWidth = windowWidth;

        for (int i = 0; i < lines.Count; i++)
        {
            string s = lines[i] ?? string.Empty;
            if (s.Length == 0)
            {
                result.Add(new VisualSegment(startLeft, string.Empty, i, 0));
                continue;
            }
            int pos = 0;
            int segIndex = 0;
            while (pos < s.Length)
            {
                int width = (segIndex == 0 ? firstWidth : windowWidth);
                if (width <= 0) width = windowWidth;
                int take = Math.Min(width, s.Length - pos);
                string segText = s.Substring(pos, take);
                int left = (segIndex == 0 ? startLeft : 0);
                result.Add(new VisualSegment(left, segText, i, pos));
                pos += take;
                segIndex++;
            }
        }
        return result;
    }

    // Draw and place cursor
    private static void Redraw(List<string> lines, int startLeft, ref int startTop, int curLine, int curCol, bool insertMode, ref List<VisualSegment> prevRender)
    {
        int windowWidth = Console.WindowWidth;
        List<VisualSegment> segs = BuildVisualMap(lines, startLeft, windowWidth);

        EnsureSpaceForRows(ref startTop, segs.Count);

        // draw segments
        for (int row = 0; row < segs.Count; row++)
        {
            VisualSegment seg = segs[row];
            try
            {
                Console.SetCursorPosition(seg.Left, startTop + row);
            }
            catch (ArgumentOutOfRangeException)
            {
                EnsureSpaceForRows(ref startTop, segs.Count);
                Console.SetCursorPosition(seg.Left, startTop + row);
            }
            Console.Write(seg.Text);

            int prevLen = 0;
            if (row < prevRender.Count) prevLen = prevRender[row].Text.Length;
            if (prevLen > seg.Text.Length)
            {
                Console.Write(new string(' ', prevLen - seg.Text.Length));
            }
        }

        // clear leftover previous rows
        for (int row = segs.Count; row < prevRender.Count; row++)
        {
            try
            {
                Console.SetCursorPosition(prevRender[row].Left, startTop + row);
                Console.Write(new string(' ', prevRender[row].Text.Length));
            }
            catch
            {
            }
        }

        // save render
        prevRender = segs;

        // draw insert/overwrite indicator at end of status area? (not implemented)
        // Position cursor
        int cursorLeft, cursorTop;
        GetCursorVisualPosition(lines, curLine, curCol, startLeft, startTop, windowWidth, out cursorLeft, out cursorTop);
        try
        {
            Console.SetCursorPosition(cursorLeft, cursorTop);
        }
        catch (ArgumentOutOfRangeException)
        {
            EnsureSpaceForRows(ref startTop, segs.Count);
            GetCursorVisualPosition(lines, curLine, curCol, startLeft, startTop, windowWidth, out cursorLeft, out cursorTop);
            try { Console.SetCursorPosition(cursorLeft, cursorTop); } catch { }
        }
    }

    // Ensure console buffer has space for required rows starting at startTop
    private static void EnsureSpaceForRows(ref int startTop, int requiredRows)
    {
        if (requiredRows < 0) requiredRows = 0;
        while (startTop + requiredRows >= Console.BufferHeight)
        {
            try { Console.SetCursorPosition(0, Console.BufferHeight - 1); } catch { }
            Console.WriteLine();
            try { startTop = Console.CursorTop - requiredRows; } catch { startTop = Math.Max(0, Console.BufferHeight - requiredRows - 1); }
            if (startTop < 0) startTop = 0;
        }
    }

    // Map logical cursor to visual coords
    private static void GetCursorVisualPosition(List<string> lines, int curLine, int curCol, int startLeft, int startTop, int windowWidth, out int outLeft, out int outTop)
    {
        outLeft = startLeft;
        outTop = startTop;
        if (curLine < 0) curLine = 0;
        if (curLine >= lines.Count) curLine = lines.Count - 1;
        if (curCol < 0) curCol = 0;
        if (curCol > lines[curLine].Length) curCol = lines[curLine].Length;

        int firstWidth = windowWidth - startLeft;
        if (firstWidth <= 0) firstWidth = windowWidth;

        int visualRow = 0;
        for (int i = 0; i < curLine; i++)
        {
            string s = lines[i] ?? string.Empty;
            if (s.Length == 0) { visualRow += 1; continue; }
            int pos = 0;
            int segIndex = 0;
            while (pos < s.Length)
            {
                int width = (segIndex == 0 ? firstWidth : windowWidth);
                if (width <= 0) width = windowWidth;
                int take = Math.Min(width, s.Length - pos);
                pos += take;
                segIndex++;
            }
            visualRow += segIndex;
        }

        string curLineStr = lines[curLine] ?? string.Empty;
        if (curLineStr.Length == 0)
        {
            outLeft = startLeft;
            outTop = startTop + visualRow;
            return;
        }

        int posInLine = 0;
        int segIdxInLine = 0;
        int pos = 0;
        while (pos < curLineStr.Length)
        {
            int width = (segIdxInLine == 0 ? firstWidth : windowWidth);
            if (width <= 0) width = windowWidth;
            int take = Math.Min(width, curLineStr.Length - pos);
            if (curCol <= pos + take)
            {
                posInLine = pos;
                break;
            }
            pos += take;
            segIdxInLine++;
        }

        if (pos >= curLineStr.Length && curCol >= curLineStr.Length)
        {
            // place at end of last segment
            int totalSegments = 0;
            pos = 0;
            segIdxInLine = 0;
            while (pos < curLineStr.Length)
            {
                totalSegments++;
                int width = (segIdxInLine == 0 ? firstWidth : windowWidth);
                if (width <= 0) width = windowWidth;
                int take = Math.Min(width, curLineStr.Length - pos);
                pos += take;
                if (pos < curLineStr.Length) segIdxInLine++;
                else break;
            }
            pos = 0;
            int idx = 0;
            while (idx < segIdxInLine)
            {
                int width = (idx == 0 ? firstWidth : windowWidth);
                if (width <= 0) width = windowWidth;
                int take = Math.Min(width, curLineStr.Length - pos);
                pos += take;
                idx++;
            }
            posInLine = pos;
        }

        int offsetInSegment = curCol - posInLine;
        int leftBase = (segIdxInLine == 0 ? startLeft : 0);
        outLeft = leftBase + offsetInSegment;
        outTop = startTop + visualRow + segIdxInLine;
        if (outLeft >= windowWidth)
        {
            outLeft -= windowWidth;
            outTop += 1;
        }
    }

    // Build visual map public wrapper
    private static List<VisualSegment> BuildVisualMap(List<string> lines, int startLeft, int windowWidth)
    {
        return BuildVisualMap(lines, startLeft, windowWidth); // overloaded - but resolves to same
    }

    // Join lines to single string
    private static string JoinLines(List<string> lines)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0) sb.Append(Environment.NewLine);
            sb.Append(lines[i]);
        }
        return sb.ToString();
    }

    // Helper: clone list of strings
    private static List<string> CloneLines(List<string> src)
    {
        List<string> dst = new List<string>(src.Count);
        for (int i = 0; i < src.Count; i++) dst.Add(src[i] ?? string.Empty);
        return dst;
    }

    // ---------------------------
    // Reflection helper for CancelKeyPress handlers
    // ---------------------------
    private static class CancelHandlersReflectionHelper
    {
        public class CancelHandlersSnapshot
        {
            public FieldInfo BackingField;
            public Delegate SavedDelegate;
        }

        // Try to find the private static field that holds CancelKeyPress invocation list.
        // Attempt several likely names and fall back to scanning static non-public fields of delegate type.
        private static FieldInfo FindCancelField()
        {
            Type t = typeof(Console);
            // Common possible field names across runtimes
            string[] names = new string[] { "CancelKeyPress", "s_cancelKeyPress", "s_CancelKeyPress", "cancelKeyPress", "CancelKeyPressEvent", "cancelKeyPressEvent" };
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo f = t.GetField(names[i], BindingFlags.Static | BindingFlags.NonPublic);
                if (f != null && typeof(Delegate).IsAssignableFrom(f.FieldType)) return f;
            }
            // fallback: find any static non-public delegate field with 'Cancel' in name
            FieldInfo[] fields = t.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (!typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                string nm = f.Name ?? string.Empty;
                if (nm.IndexOf("Cancel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    nm.IndexOf("CancelKeyPress", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return f;
                }
            }
            // last resort: return null (cannot access)
            return null;
        }

        // Detach all handlers except the one provided (keepHandler). Returns a snapshot to restore later.
        public static CancelHandlersSnapshot DetachAllExcept(ConsoleCancelEventHandler keepHandler)
        {
            try
            {
                FieldInfo f = FindCancelField();
                if (f == null) return null;
                Delegate current = (Delegate)f.GetValue(null);
                if (current == null) return new CancelHandlersSnapshot() { BackingField = f, SavedDelegate = null };

                // Save snapshot
                CancelHandlersSnapshot snap = new CancelHandlersSnapshot() { BackingField = f, SavedDelegate = current };

                // Remove all handlers except keepHandler
                Delegate[] list = current.GetInvocationList();
                for (int i = 0; i < list.Length; i++)
                {
                    Delegate d = list[i];
                    // compare by Method and Target to be safer than ReferenceEquals
                    if (keepHandler != null && IsSameDelegate(d, keepHandler)) continue;
                    try
                    {
                        Console.CancelKeyPress -= (ConsoleCancelEventHandler)d;
                    }
                    catch
                    {
                        // ignore
                    }
                }
                return snap;
            }
            catch
            {
                return null;
            }
        }

        // Restore previously saved snapshot: re-subscribe all saved delegates
        public static void RestoreSnapshot(CancelHandlersSnapshot snap)
        {
            if (snap == null) return;
            if (snap.SavedDelegate == null) return;
            Delegate[] list = snap.SavedDelegate.GetInvocationList();
            for (int i = 0; i < list.Length; i++)
            {
                Delegate d = list[i];
                try
                {
                    Console.CancelKeyPress += (ConsoleCancelEventHandler)d;
                }
                catch
                {
                    // ignore
                }
            }
        }

        // Compare delegate equality by target and method info
        private static bool IsSameDelegate(Delegate a, Delegate b)
        {
            if (a == null || b == null) return false;
            if (a.Method != b.Method) return false;
            if (a.Target != b.Target) return false;
            return true;
        }
    }
}
