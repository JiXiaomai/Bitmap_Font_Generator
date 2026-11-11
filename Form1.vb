Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Text

Public Class Form1

    ' 图案表数据：字符与字形图片的对应关系（与 GlyphCanvas1.Cells 指向同一个集合）
    Private ReadOnly cells As New List(Of GlyphCell)

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 让画布直接操作本窗体的格子集合
        GlyphCanvas1.Cells = cells
        GlyphCanvas1.CellSize = 56

        PanelAuto.AllowDrop = True
        UpdateStatus()
    End Sub

    ' ===================== ① 生成图案表 =====================

    ''' <summary>把文本按行切分，换行符本身不是字符</summary>
    Private Shared Function SplitLines(text As String) As List(Of String)
        Dim raw = text.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(CChar(vbLf))
        ' 空行不占位（不会产生空行格子）
        Return raw.Where(Function(l) l.Length > 0).ToList()
    End Function

    Private Sub ButtonGenTable_Click(sender As Object, e As EventArgs) Handles ButtonGenTable.Click
        Dim lines = SplitLines(TextChars.Text)
        If lines.Count = 0 Then
            MessageBox.Show("请先在【字符集】里输入字符；需要换行时直接回车。", "没有字符",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim filledBefore = cells.Where(Function(c) c.HasImage).Count()
        If filledBefore > 0 Then
            Dim r = MessageBox.Show($"重新生成图案表会清空已填好的 {filledBefore} 张图片，继续吗？",
                                    "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
            If r <> DialogResult.OK Then Return
        End If

        DisposeCellImages()
        cells.Clear()

        For r As Integer = 0 To lines.Count - 1
            For c As Integer = 0 To lines(r).Length - 1
                cells.Add(New GlyphCell With {
                    .Character = lines(r)(c),
                    .Row = r,
                    .Col = c
                })
            Next
        Next

        ' 列数取最长的一行；短行末尾自然留空，不占格子
        GlyphCanvas1.Rows = lines.Count
        GlyphCanvas1.Cols = lines.Max(Function(l) l.Length)
        GlyphCanvas1.RebuildSize()

        UpdateStatus()
    End Sub

    ' ===================== ② 自动排列：多张 PNG 按顺序填格 =====================

    ''' <summary>虚线框 + 居中提示文字（自己画，避免子控件挡住拖放）</summary>
    Private Sub PanelAuto_Paint(sender As Object, e As PaintEventArgs) Handles PanelAuto.Paint
        Dim g = e.Graphics
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Using pen As New Pen(Color.FromArgb(150, 170, 190), 1) With {.DashStyle = Drawing2D.DashStyle.Dash}
            g.DrawRectangle(pen, 4, 4, Math.Max(1, PanelAuto.Width - 9), Math.Max(1, PanelAuto.Height - 9))
        End Using

        Using f As New Font("Microsoft YaHei UI", 9.0F)
            Using br As New SolidBrush(Color.FromArgb(120, 130, 140))
                Using fmt As New StringFormat() With {.Alignment = StringAlignment.Center,
                                                      .LineAlignment = StringAlignment.Center}
                    g.DrawString("把多张 PNG 拖到这里" & vbCrLf & "按字符顺序自动填入", f, br,
                                 New RectangleF(0, 0, PanelAuto.Width, PanelAuto.Height), fmt)
                End Using
            End Using
        End Using
    End Sub

    Private Sub PanelAuto_DragEnter(sender As Object, e As DragEventArgs) _
        Handles PanelAuto.DragEnter, PanelAuto.DragOver

        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
            PanelAuto.BackColor = Color.FromArgb(225, 240, 255)
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub PanelAuto_DragLeave(sender As Object, e As EventArgs) Handles PanelAuto.DragLeave
        PanelAuto.BackColor = Color.FromArgb(247, 247, 247)
    End Sub

    Private Sub PanelAuto_DragDrop(sender As Object, e As DragEventArgs) Handles PanelAuto.DragDrop
        PanelAuto.BackColor = Color.FromArgb(247, 247, 247)

        Dim files = TryCast(e.Data.GetData(DataFormats.FileDrop), String())
        If files Is Nothing Then Return

        ' -1 表示自动：从第一个还空着的格子开始；全填满了就从头覆盖
        FillCellsFromFiles(files, -1)
    End Sub

    ' ===================== ③ 拖到具体格子：图片与字符绑定 =====================

    Private Sub GlyphCanvas1_FilesDropped(sender As Object, e As CellDropEventArgs) Handles GlyphCanvas1.FilesDropped
        If cells.Count = 0 Then
            MessageBox.Show("请先点击【生成图案表】，再往格子里拖图片。", "还没有图案表",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' 落在某个格子上：从这一格开始按顺序填；落在空白处：走自动模式
        FillCellsFromFiles(e.Files, e.StartIndex)
    End Sub

    ''' <summary>右键清除：Index = -1 清除全部，否则只清该格</summary>
    Private Sub GlyphCanvas1_ClearRequested(sender As Object, e As CellClearEventArgs) Handles GlyphCanvas1.ClearRequested
        If e.Index = -1 Then
            DisposeCellImages()
        ElseIf e.Index >= 0 AndAlso e.Index < cells.Count Then
            If cells(e.Index).Image IsNot Nothing Then
                cells(e.Index).Image.Dispose()
                cells(e.Index).Image = Nothing
            End If
        End If

        GlyphCanvas1.Invalidate()
        UpdateStatus()
    End Sub

    ''' <summary>
    ''' 格子内图片再拖动：移到目标格并覆盖其原有图片；拖到空白处（ToIndex = -1）则清空源格子。
    ''' </summary>
    Private Sub GlyphCanvas1_CellMoveRequested(sender As Object, e As CellMoveEventArgs) Handles GlyphCanvas1.CellMoveRequested
        If e.FromIndex < 0 OrElse e.FromIndex >= cells.Count Then Return
        If e.ToIndex = e.FromIndex Then Return

        Dim src = cells(e.FromIndex)
        If Not src.HasImage Then Return

        If e.ToIndex >= 0 AndAlso e.ToIndex < cells.Count Then
            Dim dst = cells(e.ToIndex)
            If dst.Image IsNot Nothing AndAlso Not ReferenceEquals(dst.Image, src.Image) Then dst.Image.Dispose()
            ' 只是换个格子挂载，图片对象本身直接搬过去
            dst.Image = src.Image
        Else
            ' 拖到空白区域：丢弃这张图
            src.Image.Dispose()
        End If

        src.Image = Nothing

        GlyphCanvas1.Invalidate()
        UpdateStatus()
    End Sub

    ''' <summary>
    ''' 把 PNG 依次填入格子。
    ''' startIndex >= 0 时从该索引开始；startIndex = -1 时从第一个空格子开始（都被填满则从 0 覆盖）。
    ''' </summary>
    Private Sub FillCellsFromFiles(files As String(), startIndex As Integer)
        If cells.Count = 0 Then
            MessageBox.Show("请先点击【生成图案表】。", "还没有图案表",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim pngs = files.Where(Function(f) Path.GetExtension(f).Equals(".png", StringComparison.OrdinalIgnoreCase)) _
                        .ToArray()
        If pngs.Length = 0 Then
            MessageBox.Show("只支持 PNG 图片。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        If startIndex < 0 Then
            startIndex = cells.FindIndex(Function(c) Not c.HasImage)
            If startIndex < 0 Then startIndex = 0
        End If

        Dim filled As Integer = 0
        Dim failed As Integer = 0
        Dim overflow As Integer = 0

        For i As Integer = 0 To pngs.Length - 1
            Dim idx = startIndex + i
            If idx >= cells.Count Then
                overflow = pngs.Length - i
                Exit For
            End If

            Dim bmp = LoadBitmapCopy(pngs(i))
            If bmp Is Nothing Then
                failed += 1
                Continue For
            End If

            If cells(idx).Image IsNot Nothing Then cells(idx).Image.Dispose()
            cells(idx).Image = bmp
            filled += 1
        Next

        GlyphCanvas1.Invalidate()
        UpdateStatus()

        If overflow > 0 OrElse failed > 0 Then
            Dim msg As New List(Of String)
            If overflow > 0 Then msg.Add($"{overflow} 张图片超出格子数量，已忽略")
            If failed > 0 Then msg.Add($"{failed} 张图片读取失败")
            LabelStatus.Text &= " · " & String.Join("；", msg)
        End If
    End Sub

    ''' <summary>读成独立位图副本，避免占用原文件</summary>
    Private Shared Function LoadBitmapCopy(path As String) As Bitmap
        Try
            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
                Using tmp As Image = Image.FromStream(fs)
                    Return New Bitmap(tmp)
                End Using
            End Using
        Catch ex As Exception
            Debug.Print($"载入失败 {path}: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ' ===================== ④ 导出 =====================

    Private Sub ButtonExport_Click(sender As Object, e As EventArgs) Handles ButtonExport.Click
        If cells.Count = 0 Then
            MessageBox.Show("图案表是空的，请先输入字符并点击【生成图案表】。", "提示",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim filled = cells.Where(Function(c) c.HasImage).ToList()
        If filled.Count = 0 Then
            MessageBox.Show("还没有任何图片。可以把 PNG 拖到【自动排列】框，或直接拖到某个格子上。", "提示",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim name As String = TextFileName.Text.Trim()
        If name.Length = 0 Then
            MessageBox.Show("请填写输出文件名。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 Then
            MessageBox.Show("文件名中包含非法字符。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' 图集单元尺寸取所有已填图片的最大宽高；行列沿用图案表的布局
        Dim cellW As Integer = filled.Max(Function(c) c.Image.Width)
        Dim cellH As Integer = filled.Max(Function(c) c.Image.Height)
        Dim cols As Integer = GlyphCanvas1.Cols
        Dim rows As Integer = GlyphCanvas1.Rows
        Dim scaleW As Integer = cols * cellW
        Dim scaleH As Integer = rows * cellH

        Dim charLines As New StringBuilder()
        Dim seen As New HashSet(Of Integer)
        Dim dup As Integer = 0

        Dim bmp As New Bitmap(Math.Max(scaleW, 1), Math.Max(scaleH, 1), PixelFormat.Format32bppArgb)
        Try
            Using g As Graphics = Graphics.FromImage(bmp)
                g.Clear(Color.Transparent)
                g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
                g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half

                For Each cell In filled
                    Dim ox = If(CheckH.Checked, (cellW - cell.Image.Width) \ 2, 0)
                    Dim oy = If(CheckV.Checked, (cellH - cell.Image.Height) \ 2, 0)
                    Dim px = cell.Col * cellW + ox
                    Dim py = cell.Row * cellH + oy

                    g.DrawImage(cell.Image, px, py, cell.Image.Width, cell.Image.Height)

                    ' 同一字符出现多次时只写一条 char，避免 .fnt 里 id 重复
                    If seen.Add(cell.CharCode) Then
                        charLines.Append($"char id={cell.CharCode} x={px} y={py} " &
                                         $"width={cell.Image.Width} height={cell.Image.Height} " &
                                         $"xoffset={ox} yoffset={oy} xadvance={cellW} page=0 chnl=15").AppendLine()
                    Else
                        dup += 1
                    End If
                Next
            End Using

            Dim sb As New StringBuilder()
            sb.AppendLine($"info face=""Arial"" size={cellH} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=0,0 outline=0")
            sb.AppendLine($"common lineHeight={cellH} base={cellH} scaleW={scaleW} scaleH={scaleH} pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4")
            sb.AppendLine($"page id=0 file=""{name}.png""")
            sb.AppendLine($"chars count={seen.Count}")
            sb.Append(charLines.ToString())

            Debug.Print(sb.ToString())

            Dim dir As String = Application.StartupPath
            Dim fntPath As String = Path.Combine(dir, name & ".fnt")
            Dim pngPath As String = Path.Combine(dir, name & ".png")

            File.WriteAllText(fntPath, sb.ToString(), New UTF8Encoding(False))
            bmp.Save(pngPath, ImageFormat.Png)

            Dim notes As New List(Of String)
            If dup > 0 Then notes.Add($"跳过 {dup} 个重复字符")
            If filled.Count < cells.Count Then notes.Add($"{cells.Count - filled.Count} 个格子未填图")

            LabelStatus.Text = $"已导出 {name}.fnt / {name}.png → {dir}" &
                               If(notes.Count > 0, "（" & String.Join("，", notes) & "）", "")
        Catch ex As Exception
            MessageBox.Show("保存失败：" & ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            bmp.Dispose()
        End Try
    End Sub

    ' ===================== 其它 =====================

    Private Sub UpdateStatus()
        If cells.Count = 0 Then
            LabelStatus.Text = "① 输入字符 → ② 生成图案表 → ③ 拖放PNG → ④ 导出字体"
            Return
        End If

        Dim filled = cells.Where(Function(c) c.HasImage).Count()
        Dim txt = $"图案表 {GlyphCanvas1.Rows} 行 × {GlyphCanvas1.Cols} 列，共 {cells.Count} 个字符 · 已填图 {filled} · 未填 {cells.Count - filled}"

        If filled > 0 Then
            Dim cw = cells.Where(Function(c) c.HasImage).Max(Function(c) c.Image.Width)
            Dim ch = cells.Where(Function(c) c.HasImage).Max(Function(c) c.Image.Height)
            Dim sw = GlyphCanvas1.Cols * cw
            Dim sh = GlyphCanvas1.Rows * ch
            txt &= $" · 预估图片 {sw}×{sh}"
            If sw > 4096 OrElse sh > 4096 Then txt &= " ⚠ 超过 4096"
        End If

        LabelStatus.Text = txt
    End Sub

    Private Sub DisposeCellImages()
        For Each c In cells
            If c.Image IsNot Nothing Then
                c.Image.Dispose()
                c.Image = Nothing
            End If
        Next
    End Sub

    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        DisposeCellImages()
    End Sub

End Class
