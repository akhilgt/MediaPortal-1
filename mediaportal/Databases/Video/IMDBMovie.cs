using System;
using MediaPortal.GUI.Library;
using MediaPortal.Util;

namespace MediaPortal.Video.Database
{
	/// <summary>
	/// @ 23.09.2004 by FlipGer
	/// new attribute string m_strDatabase, holds for example IMDB or OFDB
	/// </summary>
	public class IMDBMovie
	{
		int					m_id=-1;
    string 			m_strDirector=String.Empty;
    string 			m_strWritingCredits=String.Empty;
    string 			m_strGenre=String.Empty;
    string 			m_strTagLine=String.Empty;
    string 			m_strPlotOutline=String.Empty;
    string 			m_strPlot=String.Empty;
    string 			m_strPictureURL=String.Empty;
    string 			m_strTitle=String.Empty;
    string 			m_strVotes=String.Empty;
    string 			m_strCast=String.Empty;
    string 			m_strSearchString=String.Empty;
    string 			m_strFile=String.Empty;
    string 			m_strPath=String.Empty;
    string 			m_strDVDLabel=String.Empty;
    string 			m_strIMDBNumber=String.Empty;
	string 			m_strDatabase=String.Empty;
    string      m_strCDLabel=String.Empty;
    int				 m_iTop250=0;
    int    		 m_iYear=1900;
    float  		 m_fRating=0.0f;
    string     m_strMPARating=String.Empty;
		int        m_iRunTime=0;
		int        m_iWatched=0;
		int				 m_actorid=-1;
		int				 m_genreid=-1;
		string		 m_strActor=String.Empty;
		string		 m_strgenre=String.Empty;
		public IMDBMovie()
		{
		}
		public int ID
		{
			get { return m_id;}
			set { m_id=value;}
		}
		public int actorId
		{
			get { return m_actorid;}
			set { m_actorid=value;}
		}
		public int genreId
		{
			get { return m_genreid;}
			set { m_genreid=value;}
		}
		public string Genre
		{
			get { return m_strgenre;}
			set { m_strgenre=value;}
		}
		public string Actor
		{
			get { return m_strActor;}
			set { m_strActor=value;}
		}
		public int RunTime
		{
			get { return m_iRunTime;}
			set { m_iRunTime=value;}
		}
		public int Watched
		{
			get { return m_iWatched;}
			set { m_iWatched=value;}
		}
		public string MPARating
		{
			get { return m_strMPARating;}
			set { m_strMPARating=value;}
		}
		public string Director
    {
      get { return m_strDirector;}
      set { m_strDirector=value;}
    }
    public string WritingCredits
    {
      get { return m_strWritingCredits;}
      set { m_strWritingCredits=value;}
    }
    public string SingleGenre
    {
      get { return m_strGenre;}
      set { m_strGenre=value;}
    }
    public string TagLine
    {
      get { return m_strTagLine;}
      set { m_strTagLine=value;}
    }
    public string PlotOutline
    {
      get { return m_strPlotOutline;}
      set { m_strPlotOutline=value;}
    }
    public string Plot
    {
      get { return m_strPlot;}
      set { m_strPlot=value;}
    }
    public string ThumbURL
    {
      get { return m_strPictureURL;}
      set { m_strPictureURL=value;}
    }
    public string Title
    {
      get { return m_strTitle;}
      set { m_strTitle=value;}
    }
    public string Votes
    {
      get { return m_strVotes;}
      set { m_strVotes=value;}
    }
    public string Cast
    {
      get { return m_strCast;}
      set { m_strCast=value;}
    }
    public string SearchString
    {
      get { return m_strSearchString;}
      set { m_strSearchString=value;}
    }
    public string File
    {
      get { return m_strFile;}
      set { m_strFile=value;}
    }
    public string Path
    {
      get { return m_strPath;}
      set { m_strPath=value;}
    }
    public string DVDLabel
    {
      get { return m_strDVDLabel;}
      set { m_strDVDLabel=value;}
    }
    
      
    public string CDLabel
    {
      get { return m_strCDLabel;}
      set { m_strCDLabel=value;}
    }
    public string IMDBNumber
    {
      get { return m_strIMDBNumber;}
      set { m_strIMDBNumber=value;}
    }
    public int Top250
    {
      get { return m_iTop250;}
      set { m_iTop250=value;}
    }
    public int Year
    {
      get { return m_iYear;}
      set { m_iYear=value;}
    }
    public float Rating
    {
      get { return m_fRating;}
      set { m_fRating=value;}
    }
	public string Database
	{
		get { return m_strDatabase;}
		set { m_strDatabase = value;}
	}
    public void Reset()
    {
      m_strDirector=String.Empty;
      m_strWritingCredits=String.Empty;
      m_strGenre=String.Empty;
      m_strTagLine=String.Empty;
      m_strPlotOutline=String.Empty;
      m_strPlot=String.Empty;
      m_strPictureURL=String.Empty;
      m_strTitle=String.Empty;
      m_strVotes=String.Empty;
      m_strCast=String.Empty;
      m_strSearchString=String.Empty;
      m_strFile=String.Empty;
      m_strPath=String.Empty;
      m_strDVDLabel=String.Empty;
      m_strIMDBNumber=String.Empty;
      m_iTop250=0;
      m_iYear=1900;
      m_fRating=0.0f;
			m_strDatabase = String.Empty;
			m_id=-1;
			m_strMPARating=String.Empty;
			m_iRunTime=0;
			m_iWatched=0;
		}
		public void SetProperties()
		{
			string strThumb = Utils.GetLargeCoverArtName(Thumbs.MovieTitle,Title);
			GUIPropertyManager.SetProperty("#director",Director);
			GUIPropertyManager.SetProperty("#genre",Genre);
			GUIPropertyManager.SetProperty("#cast",Cast);
			GUIPropertyManager.SetProperty("#dvdlabel",DVDLabel);
			GUIPropertyManager.SetProperty("#imdbnumber",IMDBNumber);
			GUIPropertyManager.SetProperty("#file",File);
			GUIPropertyManager.SetProperty("#plot",Plot);
			GUIPropertyManager.SetProperty("#plotoutline",PlotOutline);
			GUIPropertyManager.SetProperty("#rating",Rating.ToString());
			GUIPropertyManager.SetProperty("#tagline",TagLine);
			GUIPropertyManager.SetProperty("#votes",Votes);
			GUIPropertyManager.SetProperty("#credits",WritingCredits);
			GUIPropertyManager.SetProperty("#thumb",strThumb);
			GUIPropertyManager.SetProperty("#title",Title);
			GUIPropertyManager.SetProperty("#year",Year.ToString());
			GUIPropertyManager.SetProperty("#runtime",RunTime.ToString());
			GUIPropertyManager.SetProperty("#mpaarating",MPARating.ToString());
			string strValue="no";
			if (Watched>0) strValue="yes";
			GUIPropertyManager.SetProperty("#iswatched",strValue);
		}
	}
}
