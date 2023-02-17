﻿using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using static BossChecklist.UIElements.BossLogUIElements;
using Terraria.GameContent.ItemDropRules;
using System.Reflection;

namespace BossChecklist
{
	class BossLogUI : UIState
	{
		public BossAssistButton bosslogbutton; // The main button to open the Boss Log
		public BossLogPanel BookArea; // The main panel for the UI. All content is aligned within this area.
		public BossLogPanel PageOne; // left page content panel
		public BossLogPanel PageTwo; // right page content panel

		// The selected boss page starts out with an invalid number for the initial check
		private int BossLogPageNumber;

		/// <summary>
		/// The page number for the client's Boss Log. Changing the page value will automatically update the log to dispaly the selected page.
		/// </summary>
		public int PageNum {
			get => BossLogPageNumber;
			set {
				if (value == -3) {
					OpenProgressionModePrompt();
				}
				else { 
					UpdateSelectedPage(value, CategoryPageType);
				}
			}
		}

		//public static int PageNum = -3; // The page number the Log is currently on. 0+ is corresponded with boss pages.
		public const int Page_TableOfContents = -1;
		public const int Page_Credits = -2; // Even though its -2, the credits page appears last
		public const int Page_Prompt = -3;

		// Each Boss entry has 3 subpage categories
		public static CategoryPage CategoryPageType = CategoryPage.Record;
		public SubpageButton recordButton;
		public SubpageButton spawnButton;
		public SubpageButton lootButton;

		public SubpageButton[] AltPageButtons;
		public static RecordCategory RecordPageType = RecordCategory.PreviousAttempt;
		public static RecordCategory CompareState = RecordCategory.None; // Compare record values to one another
		//public static int[] AltPageSelected; // AltPage for Records is "Player Best/World Best(Server)"
		//public static int[] TotalAltPages; // The total amount of "subpages" for Records, Spawn, and Loot pages

		public UIImageButton NextPage;
		public UIImageButton PrevPage;
		public BookUI filterPanel;
		private List<BookUI> filterCheck;
		private List<BookUI> filterCheckMark;

		public BookUI ToCTab;
		public BookUI CreditsTab;
		public BookUI BossTab;
		public BookUI MiniBossTab;
		public BookUI EventTab;
		public BookUI InfoTab;
		public BookUI ShortcutsTab;
		public bool filterOpen = false;

		public UIList prehardmodeList;
		public UIList hardmodeList;
		public ProgressBar prehardmodeBar;
		public ProgressBar hardmodeBar;
		public BossLogUIElements.FixedUIScrollbar scrollOne;
		public BossLogUIElements.FixedUIScrollbar scrollTwo;

		public UIList pageTwoItemList; // Item slot lists that include: Loot tables, spawn item, and collectibles
		public UIImage PromptCheck;

		// Cropped Textures
		public static Asset<Texture2D> bookTexture;
		public static Asset<Texture2D> borderTexture;
		public static Asset<Texture2D> fadedTexture;
		public static Asset<Texture2D> tabTexture;
		public static Asset<Texture2D> infoTexture;
		public static Asset<Texture2D> colorTexture;
		public static Asset<Texture2D> bookUITexture;
		public static Asset<Texture2D> prevTexture;
		public static Asset<Texture2D> nextTexture;
		public static Asset<Texture2D> tocTexture;
		public static Asset<Texture2D> credTexture;
		public static Asset<Texture2D> bossNavTexture;
		public static Asset<Texture2D> minibossNavTexture;
		public static Asset<Texture2D> eventNavTexture;
		public static Asset<Texture2D> filterTexture;
		public static Asset<Texture2D> mouseTexture;
		public static Asset<Texture2D> hiddenTexture;
		public static Asset<Texture2D> cycleTexture;
		public static Asset<Texture2D> checkMarkTexture;
		public static Asset<Texture2D> xTexture;
		public static Asset<Texture2D> circleTexture;
		public static Asset<Texture2D> strikeNTexture;
		public static Asset<Texture2D> checkboxTexture;
		public static Asset<Texture2D> chestTexture;
		public static Asset<Texture2D> goldChestTexture;
		public static Rectangle slotRectRef;
		public static readonly Color faded = new Color(128, 128, 128, 128);

		public static int RecipePageNum = 0;
		public static int RecipeShown = 0;
		public static bool showHidden = false;
		internal static bool PendingToggleBossLogUI; // Allows toggling boss log visibility from methods not run during UIScale so Main.screenWidth/etc are correct for ResetUIPositioning method

		private bool bossLogVisible;
		public bool BossLogVisible {
			get => bossLogVisible;
			set {
				if (value) {
					Append(BookArea);
					Append(ShortcutsTab);
					Append(InfoTab);
					Append(ToCTab);
					Append(filterPanel);
					Append(CreditsTab);
					Append(BossTab);
					Append(MiniBossTab);
					Append(EventTab);
					Append(PageOne);
					Append(PageTwo);
				}
				else {
					RemoveChild(PageTwo);
					RemoveChild(PageOne);
					RemoveChild(EventTab);
					RemoveChild(MiniBossTab);
					RemoveChild(BossTab);
					RemoveChild(CreditsTab);
					RemoveChild(filterPanel);
					RemoveChild(ToCTab);
					RemoveChild(InfoTab);
					RemoveChild(ShortcutsTab);
					RemoveChild(BookArea);
				}
				bossLogVisible = value;
			}
		}

		public void ToggleBossLog(bool show = true) {
			// First, determine if the player has ever opened the Log before
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();

			if (show) {
				// Mark the player as having opened the Log if they have not been so already
				if (!modPlayer.hasOpenedTheBossLog) {
					modPlayer.hasOpenedTheBossLog = true; // This will only ever happen once per character

					// When opening for the first time, check if the Progression Mode prompt is enabled and provide the prompt
					// If the prompt is disabled, just set the page to the Table of Contents.
					// TODO: Disabled progression mode related stuff until it is reworked on until an issue with player data is resolved
					// TODO: remove false from if statement once fixed
					if (!BossChecklist.BossLogConfig.PromptDisabled && false) {
						PageNum = Page_Prompt; // All page logic is handled in this method, so return afterwards.
					}
					else {
						PageNum = Page_TableOfContents;
					}
				}
				else if (modPlayer.enteredWorldReset) {
					// If the Log has been opened before, check for a world change.
					// This is to reset the page from what the user previously had back to the Table of Contents when entering another world.
					modPlayer.enteredWorldReset = false;
					PageNum = Page_TableOfContents;
				}
				else {
					RefreshPageContent(); // Otherwise, just default to the last page selected
				}				

				// Update UI Element positioning before marked visible
				// This will always occur after adjusting UIScale, since the UI has to be closed in order to open up the menu options
				//ResetUIPositioning();
				Main.playerInventory = false; // hide the player inventory
			}
			else if (PageNum >= 0) {
				// If UI is closed on a new record page, remove the new record from the list
				int selectedEntryIndex = BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex;
				if (selectedEntryIndex != -1 && modPlayer.hasNewRecord.Length > 0) {
					modPlayer.hasNewRecord[selectedEntryIndex] = false;
				}
			}

			BossLogVisible = show; // Setting the state makes the UIElements append/remove making them visible/invisible
		}

		public override void OnInitialize() {
			bookTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Outline");
			borderTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Border");
			fadedTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Faded");
			colorTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Color");
			bookUITexture = ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Back");
			tabTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Tab");
			infoTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_InfoTab");

			prevTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Prev");
			nextTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Next");
			tocTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Contents");
			credTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Credits");
			bossNavTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Boss");
			minibossNavTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Miniboss");
			eventNavTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Event");
			filterTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Filter");
			mouseTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_Shortcuts");
			hiddenTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Hidden");
			cycleTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_CycleRecipe");

			checkMarkTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Check", AssetRequestMode.ImmediateLoad);
			xTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_X", AssetRequestMode.ImmediateLoad);
			circleTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Next", AssetRequestMode.ImmediateLoad);
			strikeNTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_StrikeNext", AssetRequestMode.ImmediateLoad);
			checkboxTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Box", AssetRequestMode.ImmediateLoad);
			chestTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Chest");
			goldChestTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_GoldChest");

			slotRectRef = TextureAssets.InventoryBack.Value.Bounds;

			bosslogbutton = new BossAssistButton(bookTexture, "Mods.BossChecklist.BossLog.Terms.BossLog") {
				Id = "OpenUI"
			};
			bosslogbutton.Width.Set(34, 0f);
			bosslogbutton.Height.Set(38, 0f);
			bosslogbutton.Left.Set(Main.screenWidth - bosslogbutton.Width.Pixels - 190, 0f);
			bosslogbutton.Top.Pixels = Main.screenHeight - bosslogbutton.Height.Pixels - 8;
			bosslogbutton.OnClick += (a, b) => ToggleBossLog(true);

			/* Keep incase more alt pages are reintroduced
			TotalAltPages = new int[] {
				4, // Record types have their own pages (Last, Best, First, World)
				1, // All spawn info is on one page
				1 // Loot and Collectibles occupy the same page
			};
			*/

			BookArea = new BossLogPanel();
			BookArea.Width.Pixels = bookUITexture.Value.Width;
			BookArea.Height.Pixels = bookUITexture.Value.Height;

			InfoTab = new BookUI(infoTexture) {
				Id = "Info_Tab"
			};
			InfoTab.Width.Pixels = infoTexture.Value.Width;
			InfoTab.Height.Pixels = infoTexture.Value.Height;

			ShortcutsTab = new BookUI(infoTexture) {
				Id = "Shortcut_Tab"
			};
			ShortcutsTab.Width.Pixels = infoTexture.Value.Width;
			ShortcutsTab.Height.Pixels = infoTexture.Value.Height;

			ToCTab = new BookUI(tabTexture) {
				Id = "ToCFilter_Tab"
			};
			ToCTab.Width.Pixels = tabTexture.Value.Width;
			ToCTab.Height.Pixels = tabTexture.Value.Height;
			ToCTab.OnClick += OpenViaTab;
			ToCTab.OnRightClick += (a, b) => ClearForcedDowns();

			BossTab = new BookUI(tabTexture) {
				Id = "Boss_Tab"
			};
			BossTab.Width.Pixels = tabTexture.Value.Width;
			BossTab.Height.Pixels = tabTexture.Value.Height;
			BossTab.OnClick += OpenViaTab;

			MiniBossTab = new BookUI(tabTexture) {
				Id = "Miniboss_Tab"
			};
			MiniBossTab.Width.Pixels = tabTexture.Value.Width;
			MiniBossTab.Height.Pixels = tabTexture.Value.Height;
			MiniBossTab.OnClick += OpenViaTab;

			EventTab = new BookUI(tabTexture) {
				Id = "Event_Tab"
			};
			EventTab.Width.Pixels = tabTexture.Value.Width;
			EventTab.Height.Pixels = tabTexture.Value.Height;
			EventTab.OnClick += OpenViaTab;

			CreditsTab = new BookUI(tabTexture) {
				Id = "Credits_Tab"
			};
			CreditsTab.Width.Pixels = tabTexture.Value.Width;
			CreditsTab.Height.Pixels = tabTexture.Value.Height;
			CreditsTab.OnClick += OpenViaTab;

			PageOne = new BossLogPanel() {
				Id = "PageOne",
			};
			PageOne.Width.Pixels = 375;
			PageOne.Height.Pixels = 480;

			PrevPage = new BossAssistButton(prevTexture, "") {
				Id = "Previous"
			};
			PrevPage.Width.Pixels = prevTexture.Value.Width;
			PrevPage.Height.Pixels = prevTexture.Value.Height;
			PrevPage.Left.Pixels = 8;
			PrevPage.Top.Pixels = 416;
			PrevPage.OnClick += PageChangerClicked;

			prehardmodeList = new UIList();
			prehardmodeList.Left.Pixels = 4;
			prehardmodeList.Top.Pixels = 44;
			prehardmodeList.Width.Pixels = PageOne.Width.Pixels - 60;
			prehardmodeList.Height.Pixels = PageOne.Height.Pixels - 136;
			prehardmodeList.PaddingTop = 5;

			// Order matters here
			prehardmodeBar = new ProgressBar();
			prehardmodeBar.Left.Pixels = PrevPage.Left.Pixels + PrevPage.Width.Pixels + 10;
			prehardmodeBar.Height.Pixels = 14;
			prehardmodeBar.Top.Pixels = PrevPage.Top.Pixels + (PrevPage.Height.Pixels / 2) - (prehardmodeBar.Height.Pixels / 2);
			prehardmodeBar.Width.Pixels = PageOne.Width.Pixels - (prehardmodeBar.Left.Pixels * 2);

			PageTwo = new BossLogPanel() {
				Id = "PageTwo"
			};
			PageTwo.Width.Pixels = 375;
			PageTwo.Height.Pixels = 480;

			pageTwoItemList = new UIList();

			Asset<Texture2D> filterPanelTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Filter");
			filterPanel = new BookUI(filterPanelTexture) {
				Id = "filterPanel"
			};
			filterPanel.Height.Pixels = 166;
			filterPanel.Width.Pixels = 50;

			filterCheckMark = new List<BookUI>();
			filterCheck = new List<BookUI>();

			List<Asset<Texture2D>> filterNav = new List<Asset<Texture2D>>() {
				bossNavTexture,
				minibossNavTexture,
				eventNavTexture,
				hiddenTexture
			};

			for (int i = 0; i < 4; i++) {
				BookUI newCheck = new BookUI(checkMarkTexture) {
					Id = "C_" + i
				};
				newCheck.Left.Pixels = filterNav[i].Value.Width * 0.56f;
				newCheck.Top.Pixels = filterNav[i].Value.Height * 3 / 8;
				filterCheckMark.Add(newCheck);

				BookUI newCheckBox = new BookUI(filterNav[i]) {
					Id = "F_" + i
				};
				newCheckBox.Top.Pixels = (34 * i) + 15;
				newCheckBox.Left.Pixels = (25) - (filterNav[i].Value.Width / 2);
				newCheckBox.OnClick += ChangeFilter;
				if (filterNav[i] == hiddenTexture) {
					newCheckBox.OnRightClick += (a, b) => ClearHiddenList();
				}
				newCheckBox.Append(filterCheckMark[i]);
				filterCheck.Add(newCheckBox);
			}

			// Setup the inital checkmarks to display what the user has prematurely selected
			Filters_SetImage();

			// Append the filter checks to the filter panel
			foreach (BookUI uiimage in filterCheck) {
				filterPanel.Append(uiimage);
			}

			NextPage = new BossAssistButton(nextTexture, "") {
				Id = "Next"
			};
			NextPage.Width.Pixels = nextTexture.Value.Width;
			NextPage.Height.Pixels = nextTexture.Value.Height;
			NextPage.Left.Pixels = PageTwo.Width.Pixels - NextPage.Width.Pixels - 12;
			NextPage.Top.Pixels = 416;
			NextPage.OnClick += PageChangerClicked;
			PageTwo.Append(NextPage);

			hardmodeList = new UIList();
			hardmodeList.Left.Pixels = 19;
			hardmodeList.Top.Pixels = 44;
			hardmodeList.Width.Pixels = PageOne.Width.Pixels - 60;
			hardmodeList.Height.Pixels = PageOne.Height.Pixels - 136;
			hardmodeList.PaddingTop = 5;

			// Order matters here
			hardmodeBar = new ProgressBar();
			hardmodeBar.Left.Pixels = NextPage.Left.Pixels - 10 - prehardmodeBar.Width.Pixels;
			hardmodeBar.Height.Pixels = 14;
			hardmodeBar.Top.Pixels = NextPage.Top.Pixels + (NextPage.Height.Pixels / 2) - (hardmodeBar.Height.Pixels / 2);
			hardmodeBar.Width.Pixels = prehardmodeBar.Width.Pixels;

			recordButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.Records");
			recordButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			recordButton.Height.Pixels = 25;
			recordButton.Left.Pixels = PageTwo.Width.Pixels / 2 - recordButton.Width.Pixels - 8;
			recordButton.Top.Pixels = 15;
			recordButton.OnClick += (a, b) => UpdateSelectedPage(PageNum, CategoryPage.Record);
			recordButton.OnRightClick += (a, b) => ResetStats();

			spawnButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.SpawnInfo");
			spawnButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			spawnButton.Height.Pixels = 25;
			spawnButton.Left.Pixels = PageTwo.Width.Pixels / 2 + 8;
			spawnButton.Top.Pixels = 15;
			spawnButton.OnClick += (a, b) => UpdateSelectedPage(PageNum, CategoryPage.Spawn);

			lootButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.LootCollect");
			lootButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24 + 16;
			lootButton.Height.Pixels = 25;
			lootButton.Left.Pixels = PageTwo.Width.Pixels / 2 - lootButton.Width.Pixels / 2;
			lootButton.Top.Pixels = 50;
			lootButton.OnClick += (a, b) => UpdateSelectedPage(PageNum, CategoryPage.Loot);
			lootButton.OnRightClick += RemoveItem;

			// These will serve as a reservation for our AltPage buttons
			SubpageButton PrevRecordButton = new SubpageButton((int)RecordCategory.PreviousAttempt);
			PrevRecordButton.OnClick += (a, b) => HandleRecordTypeButton(RecordCategory.PreviousAttempt);
			PrevRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(RecordCategory.PreviousAttempt, false);

			SubpageButton FirstRecordButton = new SubpageButton((int)RecordCategory.FirstRecord);
			FirstRecordButton.OnClick += (a, b) => HandleRecordTypeButton(RecordCategory.FirstRecord);
			FirstRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(RecordCategory.FirstRecord, false);

			SubpageButton BestRecordButton = new SubpageButton((int)RecordCategory.BestRecord);
			BestRecordButton.OnClick += (a, b) => HandleRecordTypeButton(RecordCategory.BestRecord);
			BestRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(RecordCategory.BestRecord, false);

			SubpageButton WorldRecordButton = new SubpageButton((int)RecordCategory.WorldRecord);
			WorldRecordButton.OnClick += (a, b) => HandleRecordTypeButton(RecordCategory.WorldRecord);
			WorldRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(RecordCategory.WorldRecord, false);

			AltPageButtons = new SubpageButton[] {
				PrevRecordButton,
				BestRecordButton,
				FirstRecordButton,
				WorldRecordButton
			};
		}

		public override void Update(GameTime gameTime) {
			if (PendingToggleBossLogUI) {
				PendingToggleBossLogUI = false;
				ToggleBossLog(!BossLogVisible);
			}
			this.AddOrRemoveChild(bosslogbutton, Main.playerInventory);
			base.Update(gameTime);
		}

		public TextSnippet hoveredTextSnippet;
		public override void Draw(SpriteBatch spriteBatch) {
			base.Draw(spriteBatch);

			if (hoveredTextSnippet != null) {
				hoveredTextSnippet.OnHover();
				if (Main.mouseLeft && Main.mouseLeftRelease) {
					hoveredTextSnippet.OnClick();
				}
				hoveredTextSnippet = null;
			}
		}

		public void ResetUIPositioning() {
			// Reset the position of the button to make sure it updates with the screen res
			BookArea.Left.Pixels = (Main.screenWidth / 2) - (BookArea.Width.Pixels / 2);
			BookArea.Top.Pixels = (Main.screenHeight / 2) - (BookArea.Height.Pixels / 2) - 6;
			PageOne.Left.Pixels = BookArea.Left.Pixels + 20;
			PageOne.Top.Pixels = BookArea.Top.Pixels + 12;
			PageTwo.Left.Pixels = BookArea.Left.Pixels - 15 + BookArea.Width.Pixels - PageTwo.Width.Pixels;
			PageTwo.Top.Pixels = BookArea.Top.Pixels + 12;

			ShortcutsTab.Left.Pixels = BookArea.Left.Pixels + 40;
			ShortcutsTab.Top.Pixels = BookArea.Top.Pixels - infoTexture.Value.Height + 8;

			InfoTab.Left.Pixels = ShortcutsTab.Left.Pixels + ShortcutsTab.Width.Pixels - 8;
			InfoTab.Top.Pixels = ShortcutsTab.Top.Pixels;

			int offsetY = 50;

			// ToC/Filter Tab and Credits Tab never flips to the other side, just disappears when on said page
			ToCTab.Left.Pixels = BookArea.Left.Pixels - 20;
			ToCTab.Top.Pixels = BookArea.Top.Pixels + offsetY;
			CreditsTab.Left.Pixels = BookArea.Left.Pixels + BookArea.Width.Pixels - 12;
			CreditsTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 4);
			filterPanel.Top.Pixels = -5000; // throw offscreen

			// Reset book tabs Y positioning after BookArea adjusted
			BossTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 1);
			MiniBossTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 2);
			EventTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 3);
			CreditsTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 4);

			// Update the navigation tabs to the proper positions
			// This does not need to occur if the Progression prompt is shown, as they are not visible
			if (PageNum != Page_Prompt) {
				BossTab.Left.Pixels = BookArea.Left.Pixels + (PageNum >= FindNext(EntryType.Boss) || PageNum == Page_Credits ? -20 : BookArea.Width.Pixels - 12);
				MiniBossTab.Left.Pixels = BookArea.Left.Pixels + (PageNum >= FindNext(EntryType.MiniBoss) || PageNum == Page_Credits ? -20 : BookArea.Width.Pixels - 12);
				EventTab.Left.Pixels = BookArea.Left.Pixels + (PageNum >= FindNext(EntryType.Event) || PageNum == Page_Credits ? -20 : BookArea.Width.Pixels - 12);
				UpdateFilterTabPos(false); // Update filter tab visibility
			}
		}

		private void UpdateFilterTabPos(bool tabClicked) {
			if (tabClicked) {
				filterOpen = !filterOpen;
			}
			if (PageNum != Page_TableOfContents) {
				filterOpen = false;
			}

			if (filterOpen) {
				filterPanel.Top.Pixels = ToCTab.Top.Pixels;
				ToCTab.Left.Pixels = BookArea.Left.Pixels - 20 - filterPanel.Width.Pixels;
				filterPanel.Left.Pixels = ToCTab.Left.Pixels + ToCTab.Width.Pixels;
			}
			else {
				ToCTab.Left.Pixels = BookArea.Left.Pixels - 20;
				filterPanel.Top.Pixels = -5000; // throw offscreen
			}
		}

		private void Filters_SetImage() {
			// ...Bosses
			filterCheckMark[0].SetImage(BossChecklist.BossLogConfig.FilterBosses == "Show" ? checkMarkTexture : circleTexture);

			// ...Mini-Bosses
			if (BossChecklist.BossLogConfig.OnlyShowBossContent) {
				filterCheckMark[1].SetImage(xTexture);
			}
			else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Show") {
				filterCheckMark[1].SetImage(checkMarkTexture);
			}
			else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Hide") {
				filterCheckMark[1].SetImage(xTexture);
			}
			else {
				filterCheckMark[1].SetImage(circleTexture);
			}

			// ...Events
			if (BossChecklist.BossLogConfig.OnlyShowBossContent) {
				filterCheckMark[2].SetImage(xTexture);
			}
			else if (BossChecklist.BossLogConfig.FilterEvents == "Show") {
				filterCheckMark[2].SetImage(checkMarkTexture);
			}
			else if (BossChecklist.BossLogConfig.FilterEvents == "Hide") {
				filterCheckMark[2].SetImage(xTexture);
			}
			else {
				filterCheckMark[2].SetImage(circleTexture);
			}

			// ...Hidden Entries
			filterCheckMark[3].SetImage(showHidden ? checkMarkTexture : xTexture);
		}

		private void ChangeFilter(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BookUI book)
				return;

			string rowID = book.Id.Substring(2, 1);
			if (rowID == "0") {
				if (BossChecklist.BossLogConfig.FilterBosses == "Show") {
					BossChecklist.BossLogConfig.FilterBosses = "Hide when completed";
				}
				else {
					BossChecklist.BossLogConfig.FilterBosses = "Show";
				}
			}
			if (rowID == "1" && !BossChecklist.BossLogConfig.OnlyShowBossContent) {
				if (BossChecklist.BossLogConfig.FilterMiniBosses == "Show") {
					BossChecklist.BossLogConfig.FilterMiniBosses = "Hide when completed";
				}
				else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Hide when completed") {
					BossChecklist.BossLogConfig.FilterMiniBosses = "Hide";
				}
				else {
					BossChecklist.BossLogConfig.FilterMiniBosses = "Show";
				}
			}
			if (rowID == "2" && !BossChecklist.BossLogConfig.OnlyShowBossContent) {
				if (BossChecklist.BossLogConfig.FilterEvents == "Show") {
					BossChecklist.BossLogConfig.FilterEvents = "Hide when completed";
				}
				else if (BossChecklist.BossLogConfig.FilterEvents == "Hide when completed") {
					BossChecklist.BossLogConfig.FilterEvents = "Hide";
				}
				else {
					BossChecklist.BossLogConfig.FilterEvents = "Show";
				}
			}
			if (rowID == "3") {
				showHidden = !showHidden;
			}
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
			Filters_SetImage();
			RefreshPageContent();
		}

		// TODO: [??] Implement separate Reset tabs? Including: Clear Hidden List, Clear Forced Downs, Clear Records, Clear Boss Loot, etc

		private void ClearHiddenList() {
			if (!BossChecklist.DebugConfig.ResetHiddenEntries || WorldAssist.HiddenBosses.Count == 0)
				return;

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return;

			WorldAssist.HiddenBosses.Clear();
			showHidden = false;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.RequestClearHidden);
				packet.Send();
			}
			RefreshPageContent();
		}

		private void ClearForcedDowns() {
			if (!BossChecklist.DebugConfig.ResetForcedDowns || WorldAssist.ForcedMarkedEntries.Count == 0)
				return;

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return;

			WorldAssist.ForcedMarkedEntries.Clear();
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.RequestClearForceDowns);
				packet.Send();
			}
			RefreshPageContent();
		}

		// Update to allow clearing Best Records only, First Records only, and All Records (including previous, excluding world records)
		private void ResetStats() {
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return;

			if (!BossChecklist.DebugConfig.ResetRecordsBool || CategoryPageType != 0)
				return;

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return;

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			int recordIndex = BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex;
			PersonalStats stats = modPlayer.RecordsForWorld[recordIndex].stats;
			stats.kills = 0;
			stats.deaths = 0;

			stats.durationBest = -1;
			stats.durationPrev = -1;

			stats.hitsTakenBest = -1;
			stats.hitsTakenPrev = -1;
			RefreshPageContent();
		}

		/// <summary>
		/// While in debug mode, players will be able to remove obtained items from their player save data using the right-click button on the selected item slot.
		/// </summary>
		private void RemoveItem(UIMouseEvent evt, UIElement listeningElement) {
			// Alt right-click an item slot to remove that item from the selected boss page's loot/collection list
			// Alt right-click the "Loot / Collection" button to entirely clear the selected boss page's loot/collection list
			// If holding Alt while right-clicking will do the above for ALL boss lists
			if (!BossChecklist.DebugConfig.ResetLootItems || CategoryPageType != CategoryPage.Loot)
				return;

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return;

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			// If tthe page button was alt right-clicked, clear all items from the player's boss loot list
			// If an item slot was alt right-clicked, remove only that item from the boss loot list
			if (listeningElement is SubpageButton) {
				modPlayer.BossItemsCollected.Clear();
			}
			else if (listeningElement is LogItemSlot slot) {
				modPlayer.BossItemsCollected.Remove(new ItemDefinition(slot.item.type));
			}
			RefreshPageContent();
		}

		private void ChangeSpawnItem(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BossAssistButton button)
				return;

			string id = button.Id;
			if (id == "NextItem") {
				RecipePageNum++;
				RecipeShown = 0;
			}
			else if (id == "PrevItem") {
				RecipePageNum--;
				RecipeShown = 0;
			}
			else if (id.Contains("CycleItem")) {
				int index = id.IndexOf('_');
				if (RecipeShown == Convert.ToInt32(id.Substring(index + 1)) - 1) {
					RecipeShown = 0;
				}
				else {
					RecipeShown++;
				}
			}
			RefreshPageContent();
		}

		public static bool[] CalculateTableOfContents(List<BossInfo> bossList) {
			bool[] visibleList = new bool[bossList.Count];
			for (int i = 0; i < bossList.Count; i++) {
				BossInfo boss = bossList[i];
				// if the boss cannot get through the config checks, it will remain false (invisible)
				bool HideUnsupported = boss.modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported;
				bool HideUnavailable = (!boss.available()) && BossChecklist.BossLogConfig.HideUnavailable;
				bool HideHidden = boss.hidden && !showHidden;
				bool SkipNonBosses = BossChecklist.BossLogConfig.OnlyShowBossContent && boss.type != EntryType.Boss;
				if (((HideUnavailable || HideHidden) && !boss.IsDownedOrForced) || SkipNonBosses || HideUnsupported) {
					continue;
				}

				// Check filters as well
				EntryType type = boss.type;
				string bFilter = BossChecklist.BossLogConfig.FilterBosses;
				string mbFilter = BossChecklist.BossLogConfig.FilterMiniBosses;
				string eFilter = BossChecklist.BossLogConfig.FilterEvents;

				bool FilterBoss = type == EntryType.Boss && bFilter == "Hide when completed" && boss.IsDownedOrForced;
				bool FilterMiniBoss = type == EntryType.MiniBoss && (mbFilter == "Hide" || (mbFilter == "Hide when completed" && boss.IsDownedOrForced));
				bool FilterEvent = type == EntryType.Event && (eFilter == "Hide" || (eFilter == "Hide when completed" && boss.IsDownedOrForced));
				if (FilterBoss || FilterMiniBoss || FilterEvent) {
					continue;
				}

				visibleList[i] = true; // Boss will show on the Table of Contents
			}
			return visibleList;
		}

		public void OpenProgressionModePrompt() {
			BossLogPageNumber = Page_Prompt;
			ResetUIPositioning(); // Updates ui elements and tabs to be properly positioned in relation the the new pagenum
			ResetBothPages(); // Reset the content of both pages before appending new content for the page

			FittedTextPanel textBox = new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.ProgressionModeDescription");
			textBox.Width.Pixels = PageOne.Width.Pixels - 30;
			textBox.Height.Pixels = PageOne.Height.Pixels - 70;
			textBox.Left.Pixels = 10;
			textBox.Top.Pixels = 60;
			PageOne.Append(textBox);

			Asset<Texture2D> backdropTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_RecordSlot", AssetRequestMode.ImmediateLoad);
			UIImage[] backdrops = new UIImage[] {
				new UIImage(backdropTexture),
				new UIImage(backdropTexture),
				new UIImage(backdropTexture),
				new UIImage(backdropTexture)
			};

			Color bookColor = BossChecklist.BossLogConfig.BossLogColor;

			backdrops[0].OnClick += (a, b) => ContinueDisabled();
			backdrops[0].OnMouseOver += (a, b) => { backdrops[0].Color = bookColor; };
			backdrops[0].OnMouseOut += (a, b) => { backdrops[0].Color = Color.White; };

			backdrops[1].OnClick += (a, b) => ContinueEnabled();
			backdrops[1].OnMouseOver += (a, b) => { backdrops[1].Color = bookColor; };
			backdrops[1].OnMouseOut += (a, b) => { backdrops[1].Color = Color.White; };

			backdrops[2].OnClick += (a, b) => CloseAndConfigure();
			backdrops[2].OnMouseOver += (a, b) => { backdrops[2].Color = bookColor; };
			backdrops[2].OnMouseOut += (a, b) => { backdrops[2].Color = Color.White; };

			backdrops[3].OnClick += (a, b) => DisablePromptMessage();
			backdrops[3].OnMouseOver += (a, b) => { backdrops[3].Color = bookColor; };
			backdrops[3].OnMouseOut += (a, b) => { backdrops[3].Color = Color.White; };

			Asset<Texture2D>[] buttonTextures = new Asset<Texture2D>[] {
				ModContent.Request<Texture2D>($"Terraria/Images/Item_{ItemID.SteampunkGoggles}", AssetRequestMode.ImmediateLoad),
				ModContent.Request<Texture2D>($"Terraria/Images/Item_{ItemID.Blindfold}", AssetRequestMode.ImmediateLoad),
				ModContent.Request<Texture2D>($"Terraria/Images/Item_{ItemID.Wrench}", AssetRequestMode.ImmediateLoad),
				ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Box", AssetRequestMode.ImmediateLoad)
			};

			UIImage[] buttons = new UIImage[] {
				new UIImage(buttonTextures[0]),
				new UIImage(buttonTextures[1]),
				new UIImage(buttonTextures[2]),
				new UIImage(buttonTextures[3])
			};

			FittedTextPanel[] textOptions = new FittedTextPanel[] {
				new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.DisableProgressMode"),
				new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.EnableProgressMode"),
				new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.ConfigProgressMode"),
				new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.DisableProgressPrompt"),
			};

			Asset<Texture2D> check = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Check", AssetRequestMode.ImmediateLoad);
			Asset<Texture2D> x = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_X", AssetRequestMode.ImmediateLoad);

			bool config = BossChecklist.BossLogConfig.PromptDisabled;
			PromptCheck = new UIImage(config ? check : x);

			for (int i = 0; i < buttonTextures.Length; i++) {
				backdrops[i].Width.Pixels = backdropTexture.Value.Width;
				backdrops[i].Height.Pixels = backdropTexture.Value.Height;
				backdrops[i].Left.Pixels = 25;
				backdrops[i].Top.Pixels = 75 + (75 * i);

				buttons[i].Width.Pixels = buttonTextures[i].Value.Width;
				buttons[i].Height.Pixels = buttonTextures[i].Value.Height;
				buttons[i].Left.Pixels = 15;
				buttons[i].Top.Pixels = backdrops[i].Height.Pixels / 2 - buttons[i].Height.Pixels / 2;

				textOptions[i].Width.Pixels = backdrops[i].Width.Pixels - (buttons[i].Left.Pixels + buttons[i].Width.Pixels + 15);
				textOptions[i].Height.Pixels = backdrops[i].Height.Pixels;
				textOptions[i].Left.Pixels = buttons[i].Left.Pixels + buttons[i].Width.Pixels;
				textOptions[i].Top.Pixels = -10;
				textOptions[i].PaddingTop = 0;
				textOptions[i].PaddingLeft = 15;

				if (i == buttonTextures.Length - 1) {
					buttons[i].Append(PromptCheck);
				}
				backdrops[i].Append(buttons[i]);
				backdrops[i].Append(textOptions[i]);
				PageTwo.Append(backdrops[i]);
			}
		}

		public void ContinueDisabled() {
			BossChecklist.BossLogConfig.MaskTextures = false;
			BossChecklist.BossLogConfig.MaskNames = false;
			BossChecklist.BossLogConfig.UnmaskNextBoss = true;
			BossChecklist.BossLogConfig.MaskBossLoot = false;
			BossChecklist.BossLogConfig.MaskHardMode = false;
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);

			Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasOpenedTheBossLog = true;
			PageNum = Page_TableOfContents;
			ToggleBossLog(true);
		}

		public void ContinueEnabled() {
			BossChecklist.BossLogConfig.MaskTextures = true;
			BossChecklist.BossLogConfig.MaskNames = true;
			BossChecklist.BossLogConfig.UnmaskNextBoss = false;
			BossChecklist.BossLogConfig.MaskBossLoot = true;
			BossChecklist.BossLogConfig.MaskHardMode = true;
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);

			Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasOpenedTheBossLog = true;
			PageNum = Page_TableOfContents;
			ToggleBossLog(true);
		}

		public void CloseAndConfigure() {
			ToggleBossLog(false);
			Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasOpenedTheBossLog = true;
			PageNum = Page_TableOfContents;

			// A whole bunch of janky code to show the config and scroll down.
			try {
				IngameFancyUI.CoverNextFrame();
				Main.playerInventory = false;
				Main.editChest = false;
				Main.npcChatText = "";
				Main.inFancyUI = true;

				var modConfigFieldInfo = Assembly.GetEntryAssembly().GetType("Terraria.ModLoader.UI.Interface").GetField("modConfig", BindingFlags.Static | BindingFlags.NonPublic);
				var modConfig = (UIState)modConfigFieldInfo.GetValue(null);

				Type UIModConfigType = Assembly.GetEntryAssembly().GetType("Terraria.ModLoader.Config.UI.UIModConfig");
				var SetModMethodInfo = UIModConfigType.GetMethod("SetMod", BindingFlags.Instance | BindingFlags.NonPublic);

				//Interface.modConfig.SetMod("BossChecklist", BossChecklist.BossLogConfig);
				SetModMethodInfo.Invoke(modConfig, new object[] { BossChecklist.instance, BossChecklist.BossLogConfig });
				Main.InGameUI.SetState(modConfig);

				//private UIList mainConfigList;
				//var mainConfigListFieldInfo = UIModConfigType.GetField("mainConfigList", BindingFlags.Instance | BindingFlags.NonPublic);
				//UIList mainConfigList = (UIList)mainConfigListFieldInfo.GetValue(modConfig);

				var uIScrollbarFieldInfo = UIModConfigType.GetField("uIScrollbar", BindingFlags.Instance | BindingFlags.NonPublic);
				UIScrollbar uIScrollbar = (UIScrollbar)uIScrollbarFieldInfo.GetValue(modConfig);
				uIScrollbar.GoToBottom();

				//mainConfigList.Goto(delegate (UIElement element) {
				//	if(element is UISortableElement sortableElement && sortableElement.Children.FirstOrDefault() is Terraria.ModLoader.Config.UI.ConfigElement configElement) {
				//		return configElement.TextDisplayFunction().IndexOf("Test", StringComparison.OrdinalIgnoreCase) != -1;
				//	}
				//});
			}
			catch (Exception) {
				BossChecklist.instance.Logger.Warn("Force opening ModConfig menu failed, code update required");
			}
		}

		public void DisablePromptMessage() {
			BossChecklist.BossLogConfig.PromptDisabled = !BossChecklist.BossLogConfig.PromptDisabled;
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
			if (BossChecklist.BossLogConfig.PromptDisabled) {
				PromptCheck.SetImage(ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Check"));
			}
			else {
				PromptCheck.SetImage(ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_X"));
			}
		}

		public void HandleRecordTypeButton(RecordCategory type, bool leftClick = true) {
			// Doing this in the for loop upon creating the buttons makes the altPage the max value for some reason. This method fixes it.
			if (!leftClick) {
				if (RecordPageType == type)
					return;

				if (CompareState == RecordPageType) {
					CompareState = RecordCategory.None;
				}
				else if (CompareState != type) {
					CompareState = type;
				}
				else {
					CompareState = RecordCategory.None;
				}
				//Main.NewText($"Set compare value to {CompareState}");
			}
			else {
				// If selecting the compared state altpage, reset compare state
				if (CompareState == type) {
					CompareState = RecordCategory.None;
				}
				UpdateSelectedPage(PageNum, CategoryPageType, type);
			}
		}

		internal void JumpToBossPage(int index, bool leftClick = true) {
			// If no alt key is pressed when clicking, just jump to the boss page selected
			// If an alt key was pressed when clicking, the either mark as defeated or hidden based on click state
			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt)) {
				PageNum = index;
			}
			else {
				// While holding alt, a user can interact with any boss list entry
				// Left-clicking forces a completion check on or off
				// Right-clicking hides the boss from the list
				BossInfo entry = BossChecklist.bossTracker.SortedBosses[index];
				if (leftClick) {
					if (WorldAssist.ForcedMarkedEntries.Contains(entry.Key)) {
						WorldAssist.ForcedMarkedEntries.Remove(entry.Key);
					}
					else if (!entry.downed()) {
						WorldAssist.ForcedMarkedEntries.Add(entry.Key);
					}
					if (Main.netMode == NetmodeID.MultiplayerClient) {
						ModPacket packet = BossChecklist.instance.GetPacket();
						packet.Write((byte)PacketMessageType.RequestForceDownBoss);
						packet.Write(entry.Key);
						packet.Write(entry.ForceDowned);
						packet.Send();
					}
				}
				else {
					entry.hidden = !entry.hidden;
					if (entry.hidden) {
						WorldAssist.HiddenBosses.Add(entry.Key);
					}
					else {
						WorldAssist.HiddenBosses.Remove(entry.Key);
					}
					BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
					if (Main.netMode == NetmodeID.MultiplayerClient) {
						ModPacket packet = BossChecklist.instance.GetPacket();
						packet.Write((byte)PacketMessageType.RequestHideBoss);
						packet.Write(entry.Key);
						packet.Write(entry.hidden);
						packet.Send();
					}
				}
				RefreshPageContent();
			}
		}

		private void OpenViaTab(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BookUI book)
				return;

			string id = book.Id;
			if (PageNum == Page_Prompt || !BookUI.DrawTab(id))
				return;

			if (id == "ToCFilter_Tab" && PageNum == Page_TableOfContents) {
				UpdateFilterTabPos(true);
				return;
			}

			// Remove new records when navigating from a page with a new record
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (PageNum >= 0 && BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex != -1) {
				modPlayer.hasNewRecord[BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex] = false;
			}

			if (id == "Boss_Tab") {
				PageNum = FindNext(EntryType.Boss);
			}
			else if (id == "Miniboss_Tab") {
				PageNum = FindNext(EntryType.MiniBoss);
			}
			else if (id == "Event_Tab") {
				PageNum = FindNext(EntryType.Event);
			}
			else if (id == "Credits_Tab") {
				PageNum = Page_Credits;
			}
			else {
				PageNum = Page_TableOfContents;
			}
		}

		private void PageChangerClicked(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BossAssistButton button)
				return;

			// Remove new records when navigating from a page with a new record
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (PageNum >= 0 && BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex != -1) {
				modPlayer.hasNewRecord[BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex] = false;
			}

			pageTwoItemList.Clear();
			prehardmodeList.Clear();
			hardmodeList.Clear();
			PageOne.RemoveChild(scrollOne);
			PageTwo.RemoveChild(scrollTwo);
			RecipeShown = 0;
			RecipePageNum = 0;

			int NewPageValue = PageNum;

			// Move to next/prev
			List<BossInfo> BossList = BossChecklist.bossTracker.SortedBosses;
			if (button.Id == "Next") {
				if (NewPageValue < BossList.Count - 1) {
					NewPageValue++;
				}
				else {
					NewPageValue = Page_Credits;
				}
			}
			else {
				// button is previous
				if (NewPageValue >= 0) {
					NewPageValue--;
				}
				else {
					NewPageValue = BossList.Count - 1;
				}
			}

			// If the page is hidden or unavailable, keep moving till its not or until page is at either end
			// Also check for "Only Bosses" navigation
			if (NewPageValue >= 0) {
				bool HiddenOrUnAvailable = BossList[NewPageValue].hidden || !BossList[NewPageValue].available();
				bool OnlyDisplayBosses = BossChecklist.BossLogConfig.OnlyShowBossContent && BossList[NewPageValue].type != EntryType.Boss;
				if (HiddenOrUnAvailable || OnlyDisplayBosses) {
					while (NewPageValue >= 0) {
						BossInfo currentBoss = BossList[NewPageValue];
						if (!currentBoss.hidden && currentBoss.available()) {
							if (BossChecklist.BossLogConfig.OnlyShowBossContent) {
								if (currentBoss.type == EntryType.Boss) {
									break;
								}
							}
							else {
								break;
							}
						}

						if (button.Id == "Next") {
							if (NewPageValue < BossList.Count - 1) {
								NewPageValue++;
							}
							else {
								NewPageValue = Page_Credits;
							}
						}
						else {
							// button is previous
							if (NewPageValue >= 0) {
								NewPageValue--;
							}
							else {
								NewPageValue = BossList.Count - 1;
							}
						}
					}
				}
			}

			PageNum = NewPageValue; // Once a valid page is found, change the page.
		}

		/// <summary>
		/// Updates desired page and subcategory when called. Used for buttons that use navigation.
		/// </summary>
		/// <param name="pageNum">The page you want to switch to.</param>
		/// <param name="catPage">The category page you want to set up, which includes record/event data, summoning info, and loot checklist. Defaults to the record page.</param>
		/// <param name="altPage">The alternate category page you want to display. As of now this just applies for the record category page, which includes last attempt, first record, best record, and world record.</param>
		public void UpdateSelectedPage(int pageNum, CategoryPage catPage = CategoryPage.Record, RecordCategory altPage = RecordCategory.None) {
			BossLogPageNumber = pageNum; // Directly change the BossLogPageNumber value in order to prevent an infinite loop
			
			// Only on boss pages does updating the category page matter
			if (PageNum >= 0) {
				CategoryPageType = catPage;
				if (altPage != RecordCategory.None) {
					RecordPageType = altPage;
				}
			}

			RefreshPageContent();
		}

		/// <summary>
		/// Restructures all page content and elements without changing the page or subcategory.
		/// </summary>
		public void RefreshPageContent() {
			ResetUIPositioning(); // Updates ui elements and tabs to be properly positioned in relation the the new pagenum
			ResetBothPages(); // Reset the content of both pages before appending new content for the page
			if (PageNum == Page_TableOfContents) {
				UpdateTableofContents();
			}
			else if (PageNum == Page_Credits) {
				UpdateCredits();
			}
			else {
				if (CategoryPageType == CategoryPage.Record) {
					OpenRecord();
				}
				else if (CategoryPageType == CategoryPage.Spawn) {
					OpenSpawn();
				}
				else if (CategoryPageType == CategoryPage.Loot) {
					OpenLoot();
				}
			}
		}

		private void ResetBothPages() {
			PageOne.RemoveAllChildren();
			PageTwo.RemoveAllChildren();

			scrollOne = new BossLogUIElements.FixedUIScrollbar();
			scrollOne.SetView(100f, 1000f);
			scrollOne.Top.Pixels = 50f;
			scrollOne.Left.Pixels = -18;
			scrollOne.Height.Set(-24f, 0.75f);
			scrollOne.HAlign = 1f;

			scrollTwo = new BossLogUIElements.FixedUIScrollbar();
			scrollTwo.SetView(100f, 1000f);
			scrollTwo.Top.Pixels = 50f;
			scrollTwo.Left.Pixels = -13;
			scrollTwo.Height.Set(-24f, 0.75f);
			scrollTwo.HAlign = 1f;

			// Reset all of the page's buttons
			if (PageNum != Page_Prompt) {
				if (PageNum != Page_Credits) {
					PageTwo.Append(NextPage); // Next page button can appear on any page except the Credits
				}
				if (PageNum != Page_TableOfContents) {
					PageOne.Append(PrevPage); // Prev page button can appear on any page except the Table of Contents
				}

				if (PageNum >= 0) {
					BossInfo boss = BossChecklist.bossTracker.SortedBosses[PageNum];
					if (boss.modSource != "Unknown" && !BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
						// Only bosses have records. Events will have their own page with banners of the enemies in the event.
						// Spawn and Loot pages do not have alt pages currently, so skip adding them
						bool validRecordPage = CategoryPageType != CategoryPage.Record || boss.type != EntryType.Boss;
						if (!validRecordPage) {
							PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
							int recordIndex = BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex;
							PersonalStats record = modPlayer.RecordsForWorld[recordIndex].stats;
							int totalRecords = (int)RecordCategory.None;
							for (int i = 0; i < totalRecords; i++) {
								if ((i == 1 || i == 2) && record.kills == 0)
									continue; // If a player has no kills against a boss, they can't have a First or Best record, so skip the button creation

								AltPageButtons[i].Width.Pixels = 25;
								AltPageButtons[i].Height.Pixels = 25;
								int offset = record.kills == 0 ? 0 : (i + (i < 2 ? 0 : 1) - 2) * 12;
								if (CategoryPageType == CategoryPage.Record) {
									// If First or Best buttons were skipped account for the positioning of Previous and World
									if (i < 2) {
										AltPageButtons[i].Left.Pixels = lootButton.Left.Pixels + (i - 2) * 25 + offset;
									}
									else {
										AltPageButtons[i].Left.Pixels = lootButton.Left.Pixels + lootButton.Width.Pixels + (i - 2) * 25 + offset;
									}
								}
								AltPageButtons[i].Top.Pixels = lootButton.Top.Pixels + lootButton.Height.Pixels / 2 - 11;
								PageTwo.Append(AltPageButtons[i]);
							}
						}
					}
				}
			}

			if (PageNum >= 0) {
				if (BossChecklist.bossTracker.SortedBosses[PageNum].modSource != "Unknown") {
					PageTwo.Append(spawnButton);
					PageTwo.Append(lootButton);
					//PageTwo.Append(collectButton);
					PageTwo.Append(recordButton);
				}
				else {
					UIPanel brokenPanel = new UIPanel();
					brokenPanel.Height.Pixels = 160;
					brokenPanel.Width.Pixels = 340;
					brokenPanel.Top.Pixels = 150;
					brokenPanel.Left.Pixels = 3;
					PageTwo.Append(brokenPanel);

					FittedTextPanel brokenDisplay = new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.LogFeaturesNotAvailable");
					brokenDisplay.Height.Pixels = 200;
					brokenDisplay.Width.Pixels = 340;
					brokenDisplay.Top.Pixels = -12;
					brokenDisplay.Left.Pixels = -15;
					brokenPanel.Append(brokenDisplay);
				}

				if (BossChecklist.bossTracker.SortedBosses[PageNum].modSource == "Unknown" && BossChecklist.bossTracker.SortedBosses[PageNum].npcIDs.Count == 0) {
					UIPanel brokenPanel = new UIPanel();
					brokenPanel.Height.Pixels = 160;
					brokenPanel.Width.Pixels = 340;
					brokenPanel.Top.Pixels = 150;
					brokenPanel.Left.Pixels = 14;
					PageOne.Append(brokenPanel);

					FittedTextPanel brokenDisplay = new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.NotImplemented");
					brokenDisplay.Height.Pixels = 200;
					brokenDisplay.Width.Pixels = 340;
					brokenDisplay.Top.Pixels = 0;
					brokenDisplay.Left.Pixels = -15;
					brokenPanel.Append(brokenDisplay);
				}
			}
		}

		private void UpdateTableofContents() {
			string nextBoss = "";
			prehardmodeList.Clear();
			hardmodeList.Clear();

			List<BossInfo> referenceList = BossChecklist.bossTracker.SortedBosses;
			bool[] visibleList = CalculateTableOfContents(referenceList);

			for (int bossIndex = 0; bossIndex < referenceList.Count; bossIndex++) {
				BossInfo boss = referenceList[bossIndex];
				boss.hidden = WorldAssist.HiddenBosses.Contains(boss.Key);

				// If the boss should not be visible on the Table of Contents, skip the entry in the list
				if (!visibleList[bossIndex])
					continue;

				// Setup display name. Show "???" if unavailable and Silhouettes are turned on
				string displayName = boss.DisplayName;
				BossLogConfiguration cfg = BossChecklist.BossLogConfig;

				if (nextBoss == "" && !boss.IsDownedOrForced) {
					nextBoss = boss.Key;
				}

				bool namesMasked = cfg.MaskNames && !boss.IsDownedOrForced;
				bool hardMode = cfg.MaskHardMode && !Main.hardMode && boss.progression > BossTracker.WallOfFlesh && !boss.IsDownedOrForced;
				bool availability = cfg.HideUnavailable && !boss.available() && !boss.IsDownedOrForced;
				if (namesMasked || hardMode || availability) {
					displayName = "???";
				}

				if (cfg.DrawNextMark && cfg.MaskNames && cfg.UnmaskNextBoss) {
					if (!boss.IsDownedOrForced && boss.available() && !boss.hidden && nextBoss == boss.Key) {
						displayName = boss.DisplayName;
					}
				}

				// The first boss that isnt downed to have a nextCheck will set off the next check for the rest
				// Bosses that ARE downed will still be green due to the ordering of colors within the draw method
				// Update forced downs. If the boss is actaully downed, remove the force check.
				if (boss.ForceDowned) {
					displayName += "*";
					if (boss.downed()) {
						WorldAssist.ForcedMarkedEntries.Remove(boss.Key);
					}
				}

				bool allLoot = false;
				bool allCollect = false;
				if (BossChecklist.BossLogConfig.LootCheckVisibility) {
					PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
					allLoot = allCollect = true;

					// Loop through player saved loot and boss loot to see if every item was obtained
					foreach (int loot in boss.lootItemTypes) {
						int index = boss.loot.FindIndex(x => x.itemId == loot);

						if (loot == boss.treasureBag)
							continue;

						if (index != -1 && boss.loot[index].conditions is not null) {
							bool isCorruptionLocked = WorldGen.crimson && boss.loot[index].conditions.Any(x => x is Conditions.IsCorruption || x is Conditions.IsCorruptionAndNotExpert);
							bool isCrimsonLocked = !WorldGen.crimson && boss.loot[index].conditions.Any(x => x is Conditions.IsCrimson || x is Conditions.IsCrimsonAndNotExpert);

							if (isCorruptionLocked || isCrimsonLocked)
								continue; // Skips items that are dropped within the opposing world evil
						}

						if (BossChecklist.BossLogConfig.OnlyCheckDroppedLoot && boss.collection.Contains(loot))
							continue; // If the CheckedDroppedLoot config enabled, skip loot items that are considered collectibles for the check

						Item checkItem = ContentSamples.ItemsByType[loot];
						if (!Main.expertMode && (checkItem.expert || checkItem.expertOnly))
							continue; // Skip items that are expert exclusive if not in an expert world

						if (!Main.masterMode && (checkItem.master || checkItem.masterOnly))
							continue; // Skip items that are master exclusive if not in an master world

						// If the item index is not found, end the loop and set allLoot to false
						// If this never occurs, the user successfully obtained all the items!
						if (!modPlayer.BossItemsCollected.Contains(new ItemDefinition(loot))) {
							allLoot = false; // If the item is not located in the player's obtained list, allLoot must be false
							break; // end further item checking
						}
					}

					if (boss.collection.Count == 0) {
						allCollect = allLoot; // If no collection items were setup, consider it false until all loot has been obtained
					}
					else {
						int collectCount = 0;
						foreach (int collectible in boss.collection) {
							if (collectible == -1 || collectible == 0)
								continue; // Skips empty items

							if (BossChecklist.BossLogConfig.OnlyCheckDroppedLoot && !boss.lootItemTypes.Contains(collectible)) {
								collectCount++;
								continue; // If the CheckedDroppedLoot config enabled, skip collectible items that aren't also considered loot
							}

							Item checkItem = ContentSamples.ItemsByType[collectible];
							if (!Main.expertMode && (checkItem.expert || checkItem.expertOnly))
								continue; // Skip items that are expert exclusive if not in an expert world

							if (!Main.masterMode && (checkItem.master || checkItem.masterOnly))
								continue; // Skip items that are master exclusive if not in an master world

							if (!modPlayer.BossItemsCollected.Contains(new ItemDefinition(collectible))) {
								allCollect = false; // If the item is not located in the player's obtained list, allCollect must be false
								break; // end further item checking
							}
						}

						if (collectCount == boss.collection.Count) {
							allCollect = false; // If all the items were skipped due to the DroppedLootCheck config, don't mark as all collectibles obtained
						}
					}
				}

				bool isNext = nextBoss == boss.Key && cfg.DrawNextMark;
				TableOfContents next = new TableOfContents(bossIndex, displayName, isNext, allLoot, allCollect) {
					PaddingTop = 5,
					PaddingLeft = 22 + (boss.progression <= BossTracker.WallOfFlesh ? 10 : 0)
				};
				next.OnClick += (a, b) => JumpToBossPage(next.Index);
				next.OnRightClick += (a, b) => JumpToBossPage(next.Index, false);

				if (boss.progression <= BossTracker.WallOfFlesh) {
					prehardmodeList.Add(next);
				}
				else {
					hardmodeList.Add(next);
				}
			}

			if (prehardmodeList.Count > 13) {
				PageOne.Append(scrollOne);
			}
			PageOne.Append(prehardmodeList);
			prehardmodeList.SetScrollbar(scrollOne);
			if (hardmodeList.Count > 13) {
				PageTwo.Append(scrollTwo);
			}
			PageTwo.Append(hardmodeList);
			hardmodeList.SetScrollbar(scrollTwo);

			// Calculate Progress Bar downed entries
			int[] prehDown = new int[] { 0, 0, 0 };
			int[] prehTotal = new int[] { 0, 0, 0 };
			int[] hardDown = new int[] { 0, 0, 0 };
			int[] hardTotal = new int[] { 0, 0, 0 };
			Dictionary<string, int[]> prehEntries = new Dictionary<string, int[]>();
			Dictionary<string, int[]> hardEntries = new Dictionary<string, int[]>();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				if (boss.hidden)
					continue; // The only way to manually remove entries from the progress bar is by hiding them

				if (boss.modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported)
					continue; // Unknown and Unsupported entries can be automatically removed through configs if desired

				if (boss.progression <= BossTracker.WallOfFlesh) {
					if (boss.available() || (boss.IsDownedOrForced && BossChecklist.BossLogConfig.HideUnavailable)) {
						if (!prehEntries.ContainsKey(boss.modSource)) {
							prehEntries.Add(boss.modSource, new int[] { 0, 0 });
						}
						prehTotal[(int)boss.type]++;
						prehEntries[boss.modSource][1] += 1;

						if (boss.IsDownedOrForced) {
							prehDown[(int)boss.type]++;
							prehEntries[boss.modSource][0] += 1;
						}
					}
				}
				else {
					if (boss.available() || (boss.IsDownedOrForced && BossChecklist.BossLogConfig.HideUnavailable)) {
						if (!hardEntries.ContainsKey(boss.modSource)) {
							hardEntries.Add(boss.modSource, new int[] { 0, 0 });
						}
						hardTotal[(int)boss.type]++;
						hardEntries[boss.modSource][1] += 1;

						if (boss.IsDownedOrForced) {
							if (!hardEntries.ContainsKey(boss.modSource)) {
								hardEntries.Add(boss.modSource, new int[] { 0, 0 });
							}
							hardDown[(int)boss.type]++;
							hardEntries[boss.modSource][0] += 1;
						}
					}
				}
			}

			prehardmodeBar.downedEntries = prehDown;
			prehardmodeBar.totalEntries = prehTotal;
			prehEntries.ToList().Sort((x, y) => x.Key.CompareTo(y.Key));
			prehardmodeBar.modAllEntries = prehEntries;

			hardmodeBar.downedEntries = hardDown;
			hardmodeBar.totalEntries = hardTotal;
			hardEntries.ToList().Sort((x, y) => x.Key.CompareTo(y.Key));
			hardmodeBar.modAllEntries = hardEntries;


			PageOne.Append(prehardmodeBar);
			if (!BossChecklist.BossLogConfig.MaskHardMode || Main.hardMode) {
				PageTwo.Append(hardmodeBar);
			}
		}

		private void UpdateCredits() {
			pageTwoItemList.Left.Pixels = 15;
			pageTwoItemList.Top.Pixels = 65;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 51;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - pageTwoItemList.Top.Pixels - 80;
			pageTwoItemList.Clear();

			scrollTwo.SetView(10f, 1000f);
			scrollTwo.Top.Pixels = 90;
			scrollTwo.Left.Pixels = 5;
			scrollTwo.Height.Set(-60f, 0.75f);
			scrollTwo.HAlign = 1f;

			List<string> optedMods = BossUISystem.Instance.OptedModNames;
			if (optedMods.Count > 0) {
				foreach (string mod in optedMods) {
					UIText modListed = new UIText("●" + mod, 0.85f) {
						PaddingTop = 8,
						PaddingLeft = 5
					};
					pageTwoItemList.Add(modListed);
				}
				if (optedMods.Count > 11) {
					PageTwo.Append(scrollTwo);
				}
				PageTwo.Append(pageTwoItemList);
				pageTwoItemList.SetScrollbar(scrollTwo);
			}
			else {
				// No mods are using the Log
				UIPanel brokenPanel = new UIPanel();
				brokenPanel.Height.Pixels = 220;
				brokenPanel.Width.Pixels = 340;
				brokenPanel.Top.Pixels = 120;
				brokenPanel.Left.Pixels = 18;
				PageTwo.Append(brokenPanel);

				FittedTextPanel brokenDisplay = new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.NoModsSupported");
				brokenDisplay.Height.Pixels = 200;
				brokenDisplay.Width.Pixels = 340;
				brokenDisplay.Top.Pixels = 0;
				brokenDisplay.Left.Pixels = -15;
				brokenPanel.Append(brokenDisplay);
			}
		}

		private void OpenRecord() {
			// If a boss record does not have the selected subcategory type, it should default back to previous attempt.
			if (!PageTwo.HasChild(AltPageButtons[(int)RecordPageType])) {
				RecordPageType = RecordCategory.PreviousAttempt;
			}

			if (PageNum < 0)
				return; // Code should only run if it is on an entry page

			// Currently blank, but leave here in case elements need to be added later on
		}

		/// <summary>
		/// Sets up the content needed for the spawn info page, 
		/// creating a text box containing a description of how to summon the boss or event. 
		/// It can also have an item slot that cycles through summoning items along with the item's recipe for crafting.
		/// </summary>
		private void OpenSpawn() {
			if (PageNum < 0)
				return; // Code should only run if it is on an entry page

			BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
			if (selectedBoss.modSource == "Unknown")
				return; // prevent unsupported entries from displaying info
						// a message panel will take its place notifying the user that the mods calls are out of date

			// create a message box, displaying the spawn info provided
			var message = new UIMessageBox(selectedBoss.DisplaySpawnInfo);
			message.Width.Set(-34f, 1f);
			message.Height.Set(-370f, 1f);
			message.Top.Set(85f, 0f);
			message.Left.Set(5f, 0f);
			//message.PaddingRight = 30;
			PageTwo.Append(message);

			// create a scroll bar in case the message is too long for the box to contain
			scrollTwo.SetView(100f, 1000f);
			scrollTwo.Top.Set(91f, 0f);
			scrollTwo.Height.Set(-382f, 1f);
			scrollTwo.Left.Set(-5, 0f);
			scrollTwo.HAlign = 1f;
			PageTwo.Append(scrollTwo);
			message.SetScrollbar(scrollTwo);

			// If the spawn item list is empty, inform the player that there are no summon items for the boss/event through text
			if (selectedBoss.spawnItem.Count == 0 || selectedBoss.spawnItem.All(x => x <= 0)) {
				string type = selectedBoss.type == EntryType.Boss ? "Boss" : selectedBoss.type == EntryType.Event ? "Event" : "MiniBoss";
				UIText info = new UIText(Language.GetTextValue($"Mods.BossChecklist.BossLog.DrawnText.NoSpawn{type}"));
				info.Left.Pixels = (PageTwo.Width.Pixels / 2) - (FontAssets.MouseText.Value.MeasureString(info.Text).X / 2) - 5;
				info.Top.Pixels = 300;
				PageTwo.Append(info);
				return; // since no items are listed, the recipe code does not need to occur
			}

			/// Code below handles all of the recipe searching and displaying
			// create empty lists for ingredients and required tiles
			int selectedSpawnItem = selectedBoss.spawnItem[RecipePageNum];
			List<Item> ingredients = new List<Item>();
			List<int> requiredTiles = new List<int>();
			string recipeMod = "Terraria"; // we can inform players where the recipe originates from, starting with vanilla as a base

			if (selectedSpawnItem == ItemID.None)
				return; // just in case the selected summon item is invalid

			// search for all recipes that have the item as a result
			var itemRecipes = Main.recipe
				.Take(Recipe.numRecipes)
				.Where(r => r.HasResult(selectedSpawnItem));

			// iterate through all the recipes to gather the information we need to display for the recipe
			int TotalRecipes = 0;
			foreach (Recipe recipe in itemRecipes) {
				if (TotalRecipes == RecipeShown) {
					foreach (Item item in recipe.requiredItem) {
						Item clone = item.Clone();
						OverrideForGroups(recipe, clone); // account for recipe group names
						ingredients.Add(clone); // populate the ingredients list with all items found in the required items
					}

					requiredTiles.AddRange(recipe.requiredTile); // populate the required tiles list

					if (recipe.Mod != null) {
						recipeMod = recipe.Mod.DisplayName; // if the recipe was added by a mod, credit the mod
					}
				}
				TotalRecipes++; // add to recipe count. this will be useful later for cycling through other recipes.
			}

			Item spawn = ContentSamples.ItemsByType[selectedSpawnItem]; // create an item for reference

			if (selectedBoss.Key == "Terraria TorchGod" && spawn.type == ItemID.Torch) {
				spawn.stack = 101; // apply a custom stack count for the torches needed for the Torch God summoning event
			}

			// create an item slot of the summoning item
			LogItemSlot spawnItemSlot = new LogItemSlot(spawn, false, spawn.HoverName, ItemSlot.Context.EquipDye);
			spawnItemSlot.Width.Pixels = slotRectRef.Width;
			spawnItemSlot.Height.Pixels = slotRectRef.Height;
			spawnItemSlot.Left.Pixels = 48 + (56 * 2);
			spawnItemSlot.Top.Pixels = 230;
			PageTwo.Append(spawnItemSlot);

			// If no recipes were found, skip the recipe item slot code and inform the user the item is not craftable
			if (TotalRecipes == 0) {
				string noncraftable = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Noncraftable");
				UIText craftText = new UIText(noncraftable, 0.8f);
				craftText.Left.Pixels = 10;
				craftText.Top.Pixels = 205;
				PageTwo.Append(craftText);
				return;
			}

			int row = 0; // this will track the row pos, increasing by one after the column limit is reached
			int col = 0; // this will track the column pos, increasing by one every item, and resetting to zero when the next row is made
			// To note, we do not need an item row as recipes have a max ingredient size of 14, so there is no need for a scrollbar
			foreach (Item item in ingredients) {
				// Create an item slot for the current item
				LogItemSlot ingList = new LogItemSlot(item, false, item.HoverName, ItemSlot.Context.GuideItem, 0.85f) {
					Id = $"ingredient_{item.type}"
				};
				ingList.Width.Pixels = slotRectRef.Width * 0.85f;
				ingList.Height.Pixels = slotRectRef.Height * 0.85f;
				ingList.Left.Pixels = 20 + (48 * col);
				ingList.Top.Pixels = 240 + (48 * (row + 1));
				PageTwo.Append(ingList);

				col++;
				// if col hit the max that can be drawn on the page move onto the next row
				if (col == 6) {
					if (row == 1)
						break; // Recipes should not be able to have more than 14 ingredients, so end the loop
					
					if (ingList.item.type == ItemID.None)
						break; // if the current row ends with a blank item, end the loop. this will prevent another row of blank items
					
					col = 0;
					row++;
				}
			}

			if (requiredTiles.Count == 0) {
				// If there were no tiles required for the recipe, add a 'By Hand' slot
				LogItemSlot craftItem = new LogItemSlot(new Item(ItemID.PowerGlove), false, Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.ByHand"), ItemSlot.Context.EquipArmorVanity, 0.85f);
				craftItem.Width.Pixels = slotRectRef.Width * 0.85f;
				craftItem.Height.Pixels = slotRectRef.Height * 0.85f;
				craftItem.Top.Pixels = 240 + (48 * (row + 2));
				craftItem.Left.Pixels = 20;
				PageTwo.Append(craftItem);
			}
			else if (requiredTiles.Count > 0) {
				// iterate through all required tiles to list them in item slots
				col = 0; // reset col to zero for the crafting stations
				foreach (int tile in requiredTiles) {
					if (tile == -1)
						break; // Prevents extra empty slots from being created

					string hoverText = "";
					Item craftStation = new Item(0);
					if (tile == TileID.DemonAltar) {
						// Demon altars do not have an item id, so a texture will be created solely to be drawn here
						// A demon altar will be displayed if the world evil is corruption
						// A crimson altar will be displayed if the world evil is crimson
						string demonAltar = Language.GetTextValue("MapObject.DemonAltar");
						string crimsonAltar = Language.GetTextValue("MapObject.CrimsonAltar");
						hoverText = WorldGen.crimson ? crimsonAltar : demonAltar;
					}
					else {
						// Look for items that create the tile when placed, and use that item for the item slot
						foreach (Item item in ContentSamples.ItemsByType.Values) {
							if (item.createTile == tile) {
								craftStation.SetDefaults(item.type);
								break;
							}
						}
					}

					LogItemSlot tileList = new LogItemSlot(craftStation, false, hoverText, ItemSlot.Context.EquipArmorVanity, 0.85f);
					tileList.Width.Pixels = slotRectRef.Width * 0.85f;
					tileList.Height.Pixels = slotRectRef.Height * 0.85f;
					tileList.Left.Pixels = 20 + (48 * col);
					tileList.Top.Pixels = 240 + (48 * (row + 2));
					PageTwo.Append(tileList);
					col++; // if multiple crafting stations are needed
				}
			}

			// if more than one item is used for summoning, append navigational button to cycle through the items
			// a previous item button will appear if it is not the first item listed
			if (RecipePageNum > 0) {
				BossAssistButton PrevItem = new BossAssistButton(prevTexture, "") {
					Id = "PrevItem"
				};
				PrevItem.Width.Pixels = prevTexture.Value.Width;
				PrevItem.Height.Pixels = prevTexture.Value.Width;
				PrevItem.Left.Pixels = spawnItemSlot.Left.Pixels - PrevItem.Width.Pixels - 6;
				PrevItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (PrevItem.Height.Pixels / 2);
				PrevItem.OnClick += ChangeSpawnItem;
				PageTwo.Append(PrevItem);
			}
			// a next button will appear if it is not the last item listed
			if (RecipePageNum < BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem.Count - 1) {
				BossAssistButton NextItem = new BossAssistButton(nextTexture, "") {
					Id = "NextItem"
				};
				NextItem.Width.Pixels = nextTexture.Value.Width;
				NextItem.Height.Pixels = nextTexture.Value.Height;
				NextItem.Left.Pixels = spawnItemSlot.Left.Pixels + spawnItemSlot.Width.Pixels + 6;
				NextItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (NextItem.Height.Pixels / 2);
				NextItem.OnClick += ChangeSpawnItem;
				PageTwo.Append(NextItem);
			}

			// if more than one recipe exists for the selected item, append a button that cycles through all possible recipes
			if (TotalRecipes > 1) {
				BossAssistButton CycleItem = new BossAssistButton(cycleTexture, Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.CycleRecipe")) {
					Id = "CycleItem_" + TotalRecipes
				};
				CycleItem.Width.Pixels = cycleTexture.Value.Width;
				CycleItem.Height.Pixels = cycleTexture.Value.Height;
				CycleItem.Left.Pixels = 240;
				CycleItem.Top.Pixels = 240;
				CycleItem.OnClick += ChangeSpawnItem;
				PageTwo.Append(CycleItem);
			}

			// display where the recipe originates form
			string recipeMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.RecipeFrom", recipeMod);
			UIText ModdedRecipe = new UIText(recipeMessage, 0.8f);
			ModdedRecipe.Left.Pixels = 10;
			ModdedRecipe.Top.Pixels = 205;
			PageTwo.Append(ModdedRecipe);
		}

		/// <summary>
		/// Sets up the content needed for the loot page, creating a list of item slots of the boss's loot and collectibles.
		/// </summary>
		private void OpenLoot() {
			if (PageNum < 0)
				return; // Code should only run if it is on an entry page

			// set up an item list for the listed loot itemslots
			pageTwoItemList.Left.Pixels = 0;
			pageTwoItemList.Top.Pixels = 125;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 25;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 125 - 80;
			pageTwoItemList.Clear();

			// setup the scroll for the loot item list
			scrollTwo.SetView(10f, 1000f);
			scrollTwo.Top.Pixels = 125;
			scrollTwo.Left.Pixels = -3;
			scrollTwo.Height.Set(-88f, 0.75f);
			scrollTwo.HAlign = 1f;

			BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
			List<ItemDefinition> obtainedItems = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossItemsCollected;
			List<int> bossItems = new List<int>(selectedBoss.lootItemTypes.Union(selectedBoss.collection)); // combined list of loot and collectibles
			bossItems.Remove(selectedBoss.treasureBag); // the treasurebag should not be displayed on the loot table, but drawn above it instead

			// Prevents itemslot creation for items that are dropped from within the opposite world evil, if applicable
			if (!Main.drunkWorld && !ModLoader.TryGetMod("BothEvils", out Mod mod)) {
				foreach (DropRateInfo loot in selectedBoss.loot) {
					if (loot.conditions is null)
						continue;

					bool isCorruptionLocked = WorldGen.crimson && loot.conditions.Any(x => x is Conditions.IsCorruption || x is Conditions.IsCorruptionAndNotExpert);
					bool isCrimsonLocked = !WorldGen.crimson && loot.conditions.Any(x => x is Conditions.IsCrimson || x is Conditions.IsCrimsonAndNotExpert);

					if (bossItems.Contains(loot.itemId) && (isCorruptionLocked || isCrimsonLocked)) {
						bossItems.Remove(loot.itemId);
					}
				}
			}

			int row = 0; // this will track the row pos, increasing by one after the column limit is reached
			int col = 0; // this will track the column pos, increasing by one every item, and resetting to zero when the next row is made
			LootRow newRow = new LootRow(0) {
				Id = "Loot0"
			}; // the initial row to start with

			foreach (int item in bossItems) {
				Item selectedItem = ContentSamples.ItemsByType[item];
				bool hasObtained = obtainedItems.Any(x => x.Type == item) || obtainedItems.Any(x => x.Type == item);

				// Create an item slot for the current item
				LogItemSlot itemSlot = new LogItemSlot(selectedItem, hasObtained, "", ItemSlot.Context.TrashItem) {
					Id = "loot_" + item
				};
				itemSlot.Width.Pixels = slotRectRef.Width;
				itemSlot.Height.Pixels = slotRectRef.Height;
				itemSlot.Left.Pixels = (col * 56) + 15;
				itemSlot.OnRightClick += RemoveItem; // debug functionality
				newRow.Append(itemSlot); // append the item slot to the current row

				col++; // increase col before moving on to the next item
				// if col hit the max that can be drawn on the page, add the current row to the list and move onto the next row
				if (col == 6) {
					col = 0;
					row++;
					pageTwoItemList.Add(newRow);
					newRow = new LootRow(row) {
						Id = "Loot" + row
					};
				}
			}

			// Once all our items have been added, append the final row to the list
			// This does not need to occur if col is at 0, as the current row is empty
			if (col != 0) {
				row++;
				pageTwoItemList.Add(newRow);
			}

			PageTwo.Append(pageTwoItemList); // append the list to the page so the items can be seen

			if (row > 5) {
				PageTwo.Append(scrollTwo); // If more than 5 rows exist, a scroll bar is needed to access all items
				pageTwoItemList.SetScrollbar(scrollTwo); // connect the scrollbar to the list
			}
		}

		/// <summary>
		/// Used to locate the next available entry for the player to fight that is not defeated, marked as defeated, or hidden.
		/// Mainly used for positioning and navigation.
		/// </summary>
		/// <returns>The index of the next available entry within the boss tracker.</returns>
		public static int FindNext(EntryType entryType) => BossChecklist.bossTracker.SortedBosses.FindIndex(x => !x.IsDownedOrForced && x.available() && !x.hidden && x.type == entryType);

		/// <summary> Determines if a texture should be masked by a black sihlouette. </summary>
		public static Color MaskBoss(BossInfo entry) {
			if (!entry.IsDownedOrForced) {
				if (BossChecklist.BossLogConfig.MaskTextures) {
					return Color.Black;
				}
				else if (!Main.hardMode && entry.progression > BossTracker.WallOfFlesh && BossChecklist.BossLogConfig.MaskHardMode) {
					return Color.Black;
				}
				else if (!entry.available()) {
					return Color.Black;
				}
			}
			else if (entry.hidden) {
				return Color.Black;
			}
			return Color.White;
		}

		public static void OverrideForGroups(Recipe recipe, Item item) {
			// This method taken from RecipeBrowser with permission.
			if (recipe.ProcessGroupsForText(item.type, out string nameOverride)) {
				//Main.toolTip.name = name;
			}
			if (nameOverride != "") {
				item.SetNameOverride(nameOverride);
			}
		}
	}
}
