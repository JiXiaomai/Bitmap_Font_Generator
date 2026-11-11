Imports System.Drawing

''' <summary>
''' 图案表中的一个格子：对应字符集合里的一个字符，可以挂一张字形图片。
''' 格子本身由字符决定，图片可以稍后拖入（或留空）。
''' </summary>
Public Class GlyphCell

    ''' <summary>该格子代表的字符</summary>
    Public Property Character As Char

    ''' <summary>所在行（0 起）</summary>
    Public Property Row As Integer

    ''' <summary>所在列（0 起）</summary>
    Public Property Col As Integer

    ''' <summary>挂在该格子上的字形图片；Nothing 表示还没填</summary>
    Public Property Image As Bitmap

    ''' <summary>字符编码（.fnt 里的 char id）</summary>
    Public ReadOnly Property CharCode As Integer
        Get
            Return AscW(Character)
        End Get
    End Property

    Public ReadOnly Property HasImage As Boolean
        Get
            Return Image IsNot Nothing
        End Get
    End Property

End Class
