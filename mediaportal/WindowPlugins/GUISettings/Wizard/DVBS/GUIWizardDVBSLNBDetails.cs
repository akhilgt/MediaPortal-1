using System;
using MediaPortal.GUI.Library;
using MediaPortal.TV.Recording;

namespace WindowPlugins.GUISettings.Wizard.DVBS
{
	/// <summary>
	/// Summary description for GUIWizardDVBSLNBDetails.
	/// </summary>
	public class GUIWizardDVBSLNBDetails : GUIWindow
	{
		[SkinControlAttribute(5)]			protected GUIButtonControl btnNext=null;
		[SkinControlAttribute(25)]		protected GUIButtonControl btnBack=null;
		[SkinControlAttribute(100)]		protected GUILabelControl lblLNB=null;
		[SkinControlAttribute(40)]		protected GUICheckMarkControl cmDisEqcNone=null;
		[SkinControlAttribute(41)]		protected GUICheckMarkControl cmDisEqcSimpleA=null;
		[SkinControlAttribute(42)]		protected GUICheckMarkControl cmDisEqcSimpleB=null;
		[SkinControlAttribute(43)]		protected GUICheckMarkControl cmDisEqcLevel1AA=null;
		[SkinControlAttribute(44)]		protected GUICheckMarkControl cmDisEqcLevel1BA=null;
		[SkinControlAttribute(45)]		protected GUICheckMarkControl cmDisEqcLevel1AB=null;
		[SkinControlAttribute(46)]		protected GUICheckMarkControl cmDisEqcLevel1BB=null;


		[SkinControlAttribute(50)]		protected GUICheckMarkControl cmLnb0Khz=null;
		[SkinControlAttribute(51)]		protected GUICheckMarkControl cmLnb22Khz=null;
		[SkinControlAttribute(52)]		protected GUICheckMarkControl cmLnb33Khz=null;
		[SkinControlAttribute(53)]		protected GUICheckMarkControl cmLnb44Khz=null;
		
		[SkinControlAttribute(60)]		protected GUICheckMarkControl cmLnbBandKU=null;
		[SkinControlAttribute(61)]		protected GUICheckMarkControl cmLnbBandC=null;
		[SkinControlAttribute(62)]		protected GUICheckMarkControl cmLnbBandCircular=null;
		
		
		
		int LNBNumber=1;
		int maxLNBs=1;
		int card=0;

		public GUIWizardDVBSLNBDetails()
		{
			GetID=(int)GUIWindow.Window.WINDOW_WIZARD_DVBS_SELECT_DETAILS;
		}
    
		public override bool Init()
		{
			return Load (GUIGraphicsContext.Skin+@"\wizard_tvcard_dvbs_LNB2.xml");
		}
		protected override void OnPageLoad()
		{
			base.OnPageLoad ();
			card = Int32.Parse( GUIPropertyManager.GetProperty("#WizardCard"));
			maxLNBs=Int32.Parse(GUIPropertyManager.GetProperty("#WizardsDVBSLNB"));

			TVCaptureDevice captureCard= Recorder.Get(card);
			if (captureCard!=null) 
			{
				string filename=String.Format(@"database\card_{0}.xml",captureCard.FriendlyName);

				using(MediaPortal.Profile.Settings   xmlwriter=new MediaPortal.Profile.Settings(filename))
				{
					xmlwriter.SetValueAsBool("dvbs","useLNB1",maxLNBs>=1);
					xmlwriter.SetValueAsBool("dvbs","useLNB2",maxLNBs>=2);
					xmlwriter.SetValueAsBool("dvbs","useLNB3",maxLNBs>=3);
					xmlwriter.SetValueAsBool("dvbs","useLNB4",maxLNBs>=4);
				}
			}
			Update();
		}
		void Update()
		{	
			LoadSettings();
			lblLNB.Label=String.Format("Please specify the details for LNB:{0}", LNBNumber);
		}
		protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
		{
			if (control==btnNext) 
			{
				OnNextPage();
				return;
			}
			if (control==btnBack) OnPreviousPage();
			if (controlId >=cmDisEqcNone.GetID && controlId <=cmDisEqcLevel1BB.GetID) OnDisEqC(control);
			if (controlId >=cmLnb0Khz.GetID && controlId <=cmLnb44Khz.GetID) OnLNBKhz(control);
			if (controlId >=cmLnbBandKU.GetID && controlId <=cmLnbBandCircular.GetID) Onband(control);
			base.OnClicked (controlId, control, actionType);
		}

		void Onband(GUIControl control)
		{
			cmLnbBandKU.Selected=false;
			cmLnbBandC.Selected=false;
			cmLnbBandCircular.Selected=false;
			if (control==cmLnbBandKU) cmLnbBandKU.Selected=true;
			if (control==cmLnbBandC) cmLnbBandC.Selected=true;
			if (control==cmLnbBandCircular) cmLnbBandCircular.Selected=true;
		}
		void OnLNBKhz(GUIControl control)
		{
			cmLnb0Khz.Selected=false;
			cmLnb22Khz.Selected=false;
			cmLnb33Khz.Selected=false;
			cmLnb44Khz.Selected=false;
			if (control==cmLnb0Khz) cmLnb0Khz.Selected=true;
			if (control==cmLnb22Khz) cmLnb22Khz.Selected=true;
			if (control==cmLnb33Khz) cmLnb33Khz.Selected=true;
			if (control==cmLnb44Khz) cmLnb44Khz.Selected=true;

		}
		void OnDisEqC(GUIControl control)
		{
			cmDisEqcNone.Selected=false;
			cmDisEqcSimpleA.Selected=false;
			cmDisEqcSimpleB.Selected=false;
			cmDisEqcLevel1AA.Selected=false;
			cmDisEqcLevel1BA.Selected=false;
			cmDisEqcLevel1AB.Selected=false;
			cmDisEqcLevel1BB.Selected=false;
			if (control==cmDisEqcNone) cmDisEqcNone.Selected=true;
			if (control==cmDisEqcSimpleA) cmDisEqcSimpleA.Selected=true;
			if (control==cmDisEqcSimpleB) cmDisEqcSimpleB.Selected=true;
			if (control==cmDisEqcLevel1AA) cmDisEqcLevel1AA.Selected=true;
			if (control==cmDisEqcLevel1BA) cmDisEqcLevel1BA.Selected=true;
			if (control==cmDisEqcLevel1AB) cmDisEqcLevel1AB.Selected=true;
			if (control==cmDisEqcLevel1BB) cmDisEqcLevel1BB.Selected=true;
		}

		void OnNextPage()
		{
			SaveSettings();
			if (LNBNumber < maxLNBs)
			{
				LNBNumber++;
				Update();
				return;
			}
			GUIWindowManager.ActivateWindow( (int)GUIWindow.Window.WINDOW_WIZARD_DVBS_SELECT_TRANSPONDER);
		}

		void OnPreviousPage()
		{
			SaveSettings();
			if (LNBNumber>1) LNBNumber--;
			Update();
		}
		void LoadSettings()
		{
			cmLnb0Khz.Selected=false;
			cmLnb22Khz.Selected=false;
			cmLnb33Khz.Selected=false;
			cmLnb44Khz.Selected=false;
			cmDisEqcNone.Selected=false;
			cmDisEqcSimpleA.Selected=false;
			cmDisEqcSimpleB.Selected=false;
			cmDisEqcLevel1AA.Selected=false;
			cmDisEqcLevel1BA.Selected=false;
			cmDisEqcLevel1AB.Selected=false;
			cmDisEqcLevel1BB.Selected=false;
			cmLnbBandKU.Selected=false;
			cmLnbBandC.Selected=false;
			cmLnbBandCircular.Selected=false;

			TVCaptureDevice captureCard= Recorder.Get(card);
			if (captureCard==null) return;
			string filename=String.Format(@"database\card_{0}.xml",captureCard.FriendlyName);
			
			using(MediaPortal.Profile.Settings   xmlreader=new MediaPortal.Profile.Settings(filename))
			{
				string lnbKey=String.Format("lnb{0}",LNBNumber);
				if (LNBNumber==1) lnbKey="lnb";
				int lnbKhz=xmlreader.GetValueAsInt("dvbs",lnbKey,44);
				switch (lnbKhz)
				{
					case 0: cmLnb0Khz.Selected=true;break;
					case 22: cmLnb22Khz.Selected=true;break;
					case 33: cmLnb33Khz.Selected=true;break;
					case 44: cmLnb44Khz.Selected=true;break;
				}
				
				lnbKey=String.Format("lnbKind{0}",LNBNumber);
				if (LNBNumber==1) lnbKey="lnbKind";
				int lnbKind=xmlreader.GetValueAsInt("dvbs",lnbKey,0);
				switch (lnbKind)
				{
					case 0: cmLnbBandKU.Selected=true;break;
					case 1: cmLnbBandC.Selected=true;break;
					case 2: cmLnbBandCircular.Selected=true;break;
				}

				lnbKey=String.Format("diseqc{0}",LNBNumber);
				if (LNBNumber==1) lnbKey="diseqc";
				int diseqc=xmlreader.GetValueAsInt("dvbs",lnbKey,0);
				switch (diseqc)
				{
					case 0: break;
					case 1: cmDisEqcSimpleA.Selected=true;break;
					case 2: cmDisEqcSimpleB.Selected=true;break;
					case 3: cmDisEqcLevel1AA.Selected=true;break;
					case 4: cmDisEqcLevel1BA.Selected=true;break;
					case 5: cmDisEqcLevel1AB.Selected=true;break;
					case 6: cmDisEqcLevel1BB.Selected=true;break;
				}
			}
		}

		void SaveSettings()
		{

			TVCaptureDevice captureCard= Recorder.Get(card);
			if (captureCard==null) return;
			string filename=String.Format(@"database\card_{0}.xml",captureCard.FriendlyName);
			using(MediaPortal.Profile.Settings   xmlwriter=new MediaPortal.Profile.Settings(filename))
			{
				string lnbKey=String.Format("useLNB{0}",LNBNumber);
				xmlwriter.SetValueAsBool("dvbs",lnbKey,true);
				
				lnbKey=String.Format("diseqc{0}",LNBNumber);
				if (LNBNumber==1) lnbKey="diseqc";
				int ivalue=0;
				if (cmDisEqcSimpleA.Selected)  ivalue=1;
				if (cmDisEqcSimpleB.Selected)  ivalue=2;
				if (cmDisEqcLevel1AA.Selected) ivalue=3;
				if (cmDisEqcLevel1BA.Selected) ivalue=4;
				if (cmDisEqcLevel1AB.Selected) ivalue=5;
				if (cmDisEqcLevel1BB.Selected) ivalue=6;
				xmlwriter.SetValue("dvbs",lnbKey,ivalue); 

				lnbKey=String.Format("lnb{0}",LNBNumber);
				if (LNBNumber==1) lnbKey="lnb";
				ivalue=0;
				if (cmLnb0Khz.Selected==true) ivalue=0;
				if (cmLnb22Khz.Selected==true) ivalue=22;
				if (cmLnb33Khz.Selected==true) ivalue=33;
				if (cmLnb44Khz.Selected==true) ivalue=44;
				xmlwriter.SetValue("dvbs",lnbKey,ivalue); 

				lnbKey=String.Format("lnbKind{0}",LNBNumber);
				if (LNBNumber==1) lnbKey="lnbKind";
				ivalue=0;
				if (cmLnbBandKU.Selected==true) ivalue=0;
				if (cmLnbBandC.Selected==true) ivalue=1;
				if (cmLnbBandCircular.Selected==true) ivalue=2;
				xmlwriter.SetValue("dvbs",lnbKey,ivalue);
			}
		}
	}
}
