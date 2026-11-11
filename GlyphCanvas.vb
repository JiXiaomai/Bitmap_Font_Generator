Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text

''' <summary>PNG 被拖到图案表某个格子时的事件参数</summary>
Public Class CellDropEventArgs
    Inherits EventArgs

    ''' <summary>拖入的原始文件列表</summary>
    Public ReadOnly Property Files As String()

    ''' <summary>
    ''' 落点格子在 Cells 中的索引；-1 表示落在格子之外（空白处），由调用方决定从哪一格开始填
    ''' </summary>
    Public ReadOnly Property StartIndex As Integer

    Public Sub New(files As String(), startIndex As Integer)
        Me.Files = files
        Me.StartIndex = startIndex
    End Sub
End Class

''' <summary>请求清除格子上的图片，Index = -1 表示清除全部</summary>
Public Class CellClearEventArgs
    Inherits EventArgs

    Public ReadOnly Property Index As Integer

    Public Sub New(index As Integer)
        Me.Index = index
    End Sub
End Class

''' <summary>格子内的图片被拖到另一个格子的事件参数</summary>
Public Class CellMoveEventArgs
    Inherits EventArgs

    ''' <summary>源格子索引</summary>
    Public ReadOnly Property FromIndex As Integer

    ''' <summary>目标格子索引；-1 表示拖到了空白处（相当于清空源格子）</summary>
    Public ReadOnly Property ToIndex As Integer

    Public Sub New(fromIndex As Integer, toIndex As Integer)
        Me.FromIndex = fromIndex
        Me.ToIndex = toIndex
    End Sub
End Class

''' <summary>
''' 字符图案表控件：按行列画出格子，每格中心显示字符；已被拖入图片的格子显示图片，
''' 并在左上角用小字标出对应字符（避免被图片挡住）。
''' 支持：PNG 拖到指定格子绑定、格子内图片再拖动到别的格子（移动/替换）、拖到空白处清空。
''' </summary>
Public Class GlyphCanvas
    Inherits Control

    ''' <summary>格子内图片拖动时使用的数据格式（同一个进程内传递源格子索引）</summary>
    Public Const CellImageFormat As String = "BMFontGen.CellImage"

    Private _cellSize As Integer = 56
    Private _rows As Integer
    Private _cols As Integer

    Private hoverIndex As Integer = -1
    Private contextIndex As Integer = -1
    Private _charFont As Font
    Private _badgeFont As Font
    Private _menu As ContextMenuStrip

    ' 拖动准备状态
    Private dragArmed As Boolean
    Private dragStart As Point
    Private dragIndex As Integer = -1
    Private escapePressed As Boolean

    ' 拖动中的自绘拖影：不交给系统，避免放下后残留在屏幕上
    Private movingCell As Boolean
    Private dragThumb As Bitmap
    Private dragPos As Point = New Point(Integer.MinValue, Integer.MinValue)

    ''' <summary>把 PNG 拖进图案表时触发，参数里带落点格子索引</summary>
    Public Event FilesDropped As EventHandler(Of CellDropEventArgs)

    ''' <summary>右键清除请求</summary>
    Public Event ClearRequested As EventHandler(Of CellClearEventArgs)

    ''' <summary>格子内图片被拖到别处（含拖到空白处）时触发</summary>
    Public Event CellMoveRequested As EventHandler(Of CellMoveEventArgs)

    Public Sub New()
        ' 自绘 + 双缓冲，格子多时拖动不会闪
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw, True)

        BackColor = Color.White
        _charFont = New Font("Microsoft YaHei UI", 14.0F, FontStyle.Regular, GraphicsUnit.Point)
        _badgeFont = New Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point)
        AllowDrop = True
        Cells = New List(Of GlyphCell)
        BuildContextMenu()
    End Sub

    Private Sub BuildContextMenu()
        _menu = New ContextMenuStrip()

        Dim miCell As New ToolStripMenuItem("清除此格的图片")
        Dim miAll As New ToolStripMenuItem("清除全部图片")

        AddHandler miCell.Click, Sub()
                                     If contextIndex >= 0 AndAlso Cells IsNot Nothing AndAlso contextIndex < Cells.Count Then
                                         RaiseEvent ClearRequested(Me, New CellClearEventArgs(contextIndex))
                                     End If
                                 End Sub
        AddHandler miAll.Click, Sub() RaiseEvent ClearRequested(Me, New CellClearEventArgs(-1))

        _menu.Items.Add(miCell)
        _menu.Items.Add(miAll)
        ContextMenuStrip = _menu
    End Sub

    ''' <summary>格子数据（与外部共享同一个集合引用）</summary>
    Public Property Cells As List(Of GlyphCell)

    ''' <summary>每个格子的显示边长（像素）。只影响预览，导出时按图片实际尺寸计算</summary>
    Public Property CellSize As Integer
        Get
            Return _cellSize
        End Get
        Set(value As Integer)
            _cellSize = Math.Max(24, value)
            RebuildSize()
        End Set
    End Property

    Public Property Rows As Integer
        Get
            Return _rows
        End Get
        Set(value As Integer)
            _rows = Math.Max(0, value)
            RebuildSize()
        End Set
    End Property

    Public Property Cols As Integer
        Get
            Return _cols
        End Get
        Set(value As Integer)
            _cols = Math.Max(0, value)
            RebuildSize()
        End Set
    End Property

    ''' <summary>按当前行列数调整控件尺寸（放在 AutoScroll 面板里就会自动出滚动条）</summary>
    Public Sub RebuildSize()
        If _rows = 0 OrElse _cols = 0 Then
            ' 还没建表：撑满父容器，好让提示文字显示在正中
            Dim p = Parent
            If p IsNot Nothing Then
                Dim sz As New Size(Math.Max(p.ClientSize.Width, 1), Math.Max(p.ClientSize.Height, 1))
                If Size <> sz Then Size = sz
            End If
            Invalidate()
            Return
        End If

        Dim w = Math.Max(_cols * _cellSize + 1, 1)
        Dim h = Math.Max(_rows * _cellSize + 1, 1)
        If Width <> w OrElse Height <> h Then Size = New Size(w, h)
        Invalidate()
    End Sub

    Protected Overrides Sub OnParentChanged(e As EventArgs)
        MyBase.OnParentChanged(e)
        If Parent IsNot Nothing Then AddHandler Parent.ClientSizeChanged, AddressOf ParentClientSizeChanged
        RebuildSize()
    End Sub

    Private Sub ParentClientSizeChanged(sender As Object, e As EventArgs)
        ' 空表状态下跟着父容器大小走，避免提示文字被裁掉
        If _rows = 0 OrElse _cols = 0 Then RebuildSize()
    End Sub

    ''' <summary>返回坐标点命中的格子在 Cells 中的索引，未命中返回 -1</summary>
    Public Function HitTestCell(pt As Point) As Integer
        If pt.X < 0 OrElse pt.Y < 0 Then Return -1
        Dim col = pt.X \ _cellSize
        Dim row = pt.Y \ _cellSize
        If col >= _cols OrElse row >= _rows Then Return -1
        If Cells Is Nothing Then Return -1

        For i = 0 To Cells.Count - 1
            If Cells(i).Row = row AndAlso Cells(i).Col = col Then Return i
        Next
        Return -1
    End Function

    Private Function CellRect(index As Integer) As Rectangle
        Dim c = Cells(index)
        Return New Rectangle(c.Col * _cellSize, c.Row * _cellSize, _cellSize, _cellSize)
    End Function

    ' ===================== 绘制 =====================

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.None

        If Cells Is Nothing OrElse Cells.Count = 0 Then
            DrawCenteredHint(g, "输入【字符集】后，点击【生成图案表】")
            Return
        End If

        Using gridPen As New Pen(Color.FromArgb(206, 206, 206))
            Using hoverPen As New Pen(Color.FromArgb(0, 120, 215), 2)
                Using emptyBrush As New SolidBrush(Color.FromArgb(252, 252, 252))
                    Using filledBrush As New SolidBrush(Color.FromArgb(238, 247, 240))
                        Using charBrush As New SolidBrush(Color.FromArgb(105, 105, 105))
                            Using fmt As New StringFormat() With {.Alignment = StringAlignment.Center,
                                                                  .LineAlignment = StringAlignment.Center}

                                g.FillRectangle(emptyBrush, ClientRectangle)

                                For Each cell In Cells
                                    Dim r As New Rectangle(cell.Col * _cellSize, cell.Row * _cellSize, _cellSize, _cellSize)

                                    If cell.HasImage Then
                                        g.FillRectangle(filledBrush, r)
                                        DrawGlyph(g, cell.Image, r)
                                        ' 图片可能盖住格子，字符改标在左上角
                                        DrawCharBadge(g, cell.Character, r)
                                    ElseIf Not Char.IsWhiteSpace(cell.Character) Then
                                        ' 空格等不可见字符就不画了，保持格子空着
                                        g.DrawString(cell.Character.ToString(), _charFont, charBrush, r, fmt)
                                    End If

                                    g.DrawRectangle(gridPen, r)
                                Next

                                ' 拖动中的目标格子描边
                                If hoverIndex >= 0 AndAlso hoverIndex < Cells.Count Then
                                    g.DrawRectangle(hoverPen, CellRect(hoverIndex))
                                End If

                                ' 跟随光标的拖影：自己画在画布内，不交给系统
                                DrawDragGhost(g, dragPos)
                            End Using
                        End Using
                    End Using
                End Using
            End Using
        End Using
    End Sub

    ''' <summary>把字形图片等比缩放后画在格子中央（大图缩小，小图保持原尺寸）</summary>
    Private Shared Sub DrawGlyph(g As Graphics, img As Bitmap, cell As Rectangle)
        Dim maxW As Double = cell.Width - 6
        Dim maxH As Double = cell.Height - 6
        Dim scale As Double = Math.Min(1.0, Math.Min(maxW / img.Width, maxH / img.Height))
        Dim w = Math.Max(1, CInt(Math.Round(img.Width * scale)))
        Dim h = Math.Max(1, CInt(Math.Round(img.Height * scale)))
        Dim x = cell.X + (cell.Width - w) \ 2
        Dim y = cell.Y + (cell.Height - h) \ 2

        Dim oldMode = g.InterpolationMode
        g.InterpolationMode = If(scale < 1.0, InterpolationMode.HighQualityBicubic, InterpolationMode.NearestNeighbor)
        g.DrawImage(img, New Rectangle(x, y, w, h))
        g.InterpolationMode = oldMode
    End Sub

    ''' <summary>在格子左上角标出字符：先铺一层半透明白底，保证压在图片上也看得清</summary>
    Private Sub DrawCharBadge(g As Graphics, ch As Char, cell As Rectangle)
        If Char.IsWhiteSpace(ch) Then Return

        Dim s = ch.ToString()
        Dim sz = TextRenderer.MeasureText(s, _badgeFont)
        Dim w = Math.Min(cell.Width - 2, sz.Width + 2)
        Dim h = Math.Min(cell.Height - 2, sz.Height)

        Using bg As New SolidBrush(Color.FromArgb(215, 255, 255, 255))
            g.FillRectangle(bg, cell.X + 1, cell.Y + 1, w, h)
        End Using

        TextRenderer.DrawText(g, s, _badgeFont,
                              New Rectangle(cell.X + 2, cell.Y + 1, w, h),
                              Color.FromArgb(70, 70, 70),
                              TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
    End Sub

    Private Sub DrawCenteredHint(g As Graphics, text As String)
        Using f As New Font("Microsoft YaHei UI", 10.0F)
            Using br As New SolidBrush(Color.FromArgb(150, 150, 150))
                Dim sz = g.MeasureString(text, f, Width - 20)
                g.DrawString(text, f, br, Math.Max(4, (Width - sz.Width) / 2), Math.Max(4, (Height - sz.Height) / 2))
            End Using
        End Using
    End Sub

    ' ===================== 格子内图片再拖动 =====================

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)

        If e.Button = MouseButtons.Right Then
            contextIndex = HitTestCell(e.Location)
            Return
        End If

        If e.Button = MouseButtons.Left Then
            Dim idx = HitTestCell(e.Location)
            dragIndex = idx
            dragArmed = idx >= 0 AndAlso Cells IsNot Nothing AndAlso idx < Cells.Count AndAlso Cells(idx).HasImage
            dragStart = e.Location
        End If
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)

        If Not dragArmed Then
            UpdateCursor(e.Location)
            Return
        End If

        If e.Button <> MouseButtons.Left Then
            dragArmed = False
            Return
        End If

        Dim dx = Math.Abs(e.X - dragStart.X)
        Dim dy = Math.Abs(e.Y - dragStart.Y)
        If dx < SystemInformation.DragSize.Width AndAlso dy < SystemInformation.DragSize.Height Then Return

        ' 超过拖动阈值，正式进入拖放
        dragArmed = False
        StartCellDrag(dragIndex)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        dragArmed = False
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        If Cursor IsNot Cursors.Default Then Cursor = Cursors.Default
    End Sub

    ''' <summary>悬停在已填图的格子上时给个“可移动”光标，方便发现这个操作</summary>
    Private Sub UpdateCursor(pt As Point)
        Dim idx = HitTestCell(pt)
        Dim overFilled = idx >= 0 AndAlso idx < Cells.Count AndAlso Cells(idx).HasImage
        Dim want As Cursor = If(overFilled, Cursors.SizeAll, Cursors.Default)
        If Cursor IsNot want Then Cursor = want
    End Sub

    Private Sub StartCellDrag(fromIndex As Integer)
        If Cells Is Nothing OrElse fromIndex < 0 OrElse fromIndex >= Cells.Count Then Return
        If Not Cells(fromIndex).HasImage Then Return

        escapePressed = False

        Dim data As New DataObject()
        data.SetData(CellImageFormat, False, fromIndex)

        ' 拖影由画布自己画。系统拖影是一个独立的分层窗口，放下后不一定被销毁，
        ' 于是就会有一张同样的图片留在屏幕上“悬浮”着。
        dragThumb = MakeDragThumb(Cells(fromIndex).Image)
        movingCell = True
        SetDragPoint(PointToClient(MousePosition), forceRepaint:=True)

        Try
            ' 不传 dragImage：DoDragDrop 只负责数据传递，屏幕上不会多出任何窗口
            Dim result = DoDragDrop(data, DragDropEffects.Move)

            ' 松手在任何格子之外 = 拖到空白区域 → 清空源格子（按 Esc 取消的除外）
            If result = DragDropEffects.None AndAlso Not escapePressed Then
                RaiseEvent ClearRequested(Me, New CellClearEventArgs(fromIndex))
            End If
        Finally
            ' 无论正常结束、被 Esc 取消还是异常，都把自己画的拖影收干净
            movingCell = False
            hoverIndex = -1
            If dragThumb IsNot Nothing Then
                dragThumb.Dispose()
                dragThumb = Nothing
            End If
            Invalidate()
            Update()
        End Try
    End Sub

    Protected Overrides Sub OnGiveFeedback(e As GiveFeedbackEventArgs)
        MyBase.OnGiveFeedback(e)
        ' 拖动过程中鼠标每移动一次都会回调这里，用它让拖影跟手
        If movingCell Then SetDragPoint(PointToClient(MousePosition))
    End Sub

    ''' <summary>更新拖影位置：只重画新旧两块小区域，格子多时也不掉帧</summary>
    Private Sub SetDragPoint(p As Point, Optional forceRepaint As Boolean = False)
        If p = dragPos AndAlso Not forceRepaint Then Return

        Dim oldRect = DragGhostRect()
        dragPos = p
        Dim newRect = DragGhostRect()

        If Not oldRect.IsEmpty Then Invalidate(Rectangle.Inflate(oldRect, 2, 2))
        If Not newRect.IsEmpty Then Invalidate(Rectangle.Inflate(newRect, 2, 2))
        ' 拖放循环由系统驱动，这里主动刷一次，拖影才不会滞后
        Update()
    End Sub

    ''' <summary>拖影当前占用的矩形（含描边）</summary>
    Private Function DragGhostRect() As Rectangle
        If Not movingCell OrElse dragThumb Is Nothing Then Return Rectangle.Empty
        If dragPos.X = Integer.MinValue Then Return Rectangle.Empty
        Return New Rectangle(dragPos.X - dragThumb.Width \ 2 - 1,
                             dragPos.Y - dragThumb.Height \ 2 - 1,
                             dragThumb.Width + 2, dragThumb.Height + 2)
    End Function

    ''' <summary>跟随光标的半透明缩略图；光标离开画布时不画</summary>
    Private Sub DrawDragGhost(g As Graphics, pos As Point)
        If Not movingCell OrElse dragThumb Is Nothing Then Return
        If dragPos.X = Integer.MinValue OrElse Not ClientRectangle.Contains(pos) Then Return

        Dim dst As New Rectangle(pos.X - dragThumb.Width \ 2, pos.Y - dragThumb.Height \ 2,
                                 dragThumb.Width, dragThumb.Height)

        Using attrs As New Imaging.ImageAttributes()
            Dim cm As New Imaging.ColorMatrix()
            cm.Matrix33 = 0.85F
            attrs.SetColorMatrix(cm)
            g.DrawImage(dragThumb, dst, 0, 0, dragThumb.Width, dragThumb.Height, GraphicsUnit.Pixel, attrs)
        End Using

        Using pen As New Pen(Color.FromArgb(0, 120, 215))
            g.DrawRectangle(pen, dst.X, dst.Y, dst.Width - 1, dst.Height - 1)
        End Using
    End Sub

    Protected Overrides Sub OnQueryContinueDrag(e As QueryContinueDragEventArgs)
        MyBase.OnQueryContinueDrag(e)
        If e.EscapePressed Then escapePressed = True
    End Sub

    ''' <summary>拖动时跟随鼠标的小缩略图（画在画布上，不交给系统做拖影窗口）</summary>
    Private Shared Function MakeDragThumb(img As Bitmap) As Bitmap
        If img Is Nothing Then Return Nothing

        Dim scale As Double = Math.Min(1.0, 48.0 / Math.Max(img.Width, img.Height))
        Dim w = Math.Max(1, CInt(Math.Round(img.Width * scale)))
        Dim h = Math.Max(1, CInt(Math.Round(img.Height * scale)))

        Dim bmp As New Bitmap(w, h, Imaging.PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.InterpolationMode = InterpolationMode.HighQualityBicubic
            g.DrawImage(img, 0, 0, w, h)
        End Using
        Return bmp
    End Function

    ' ===================== 拖放（外部文件 / 格子内图片） =====================

    Protected Overrides Sub OnDragEnter(e As DragEventArgs)
        MyBase.OnDragEnter(e)
        SetEffectAndHover(e)
    End Sub

    Protected Overrides Sub OnDragOver(e As DragEventArgs)
        MyBase.OnDragOver(e)
        SetEffectAndHover(e)
    End Sub

    Private Sub SetEffectAndHover(e As DragEventArgs)
        If e.Data.GetDataPresent(CellImageFormat, False) Then
            ' 格子内的图片在移动
            e.Effect = DragDropEffects.Move
            UpdateHover(e)
        ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
            UpdateHover(e)
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub UpdateHover(e As DragEventArgs)
        Dim pt = PointToClient(New Point(e.X, e.Y))
        ' 自己拖动格子内图片时，拖影也在这里跟手（万一没收到 GiveFeedback 也能动）
        If movingCell Then SetDragPoint(pt)

        Dim idx = HitTestCell(pt)
        If idx <> hoverIndex Then
            hoverIndex = idx
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnDragLeave(e As EventArgs)
        MyBase.OnDragLeave(e)
        If hoverIndex <> -1 Then
            hoverIndex = -1
            Invalidate()
        End If
        ' 光标离开画布：收掉拖影，别停在边缘
        If movingCell Then SetDragPoint(New Point(Integer.MinValue, Integer.MinValue))
    End Sub

    Protected Overrides Sub OnDragDrop(e As DragEventArgs)
        MyBase.OnDragDrop(e)
        hoverIndex = -1
        Invalidate()

        Dim target = HitTestCell(PointToClient(New Point(e.X, e.Y)))

        If e.Data.GetDataPresent(CellImageFormat, False) Then
            Dim fromIndex As Integer
            Try
                fromIndex = CInt(e.Data.GetData(CellImageFormat, False))
            Catch
                Return
            End Try
            RaiseEvent CellMoveRequested(Me, New CellMoveEventArgs(fromIndex, target))
            Return
        End If

        Dim files = TryCast(e.Data.GetData(DataFormats.FileDrop), String())
        If files Is Nothing Then Return

        RaiseEvent FilesDropped(Me, New CellDropEventArgs(files, target))
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If dragThumb IsNot Nothing Then
                dragThumb.Dispose()
                dragThumb = Nothing
            End If
            If _charFont IsNot Nothing Then
                _charFont.Dispose()
                _charFont = Nothing
            End If
            If _badgeFont IsNot Nothing Then
                _badgeFont.Dispose()
                _badgeFont = Nothing
            End If
            If _menu IsNot Nothing Then
                _menu.Dispose()
                _menu = Nothing
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
