using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using CraftableCartography.Systems;

namespace CraftableCartography.Items.Map
{
	public class DrawMapGui : GuiDialog
	{
		public static int mapSize = 32;
		private bool[,] cellStates = new bool[mapSize, mapSize];
		private int cellSize = (int)((16 * 32) / mapSize);
		private int gridPadding = 0;
		private GuiElementCellGrid cellGrid;
		private ItemSlot mapSlot;
		private ModSystemDrawableMap mapModSys;
		private bool isMouseDown = false;
		private bool? lastPaintColor = null; // null - не установлен, true - черный, false - белый
		private string mapTitle = "";

		public override string ToggleKeyCombinationCode => "drawmapgui";

		public DrawMapGui(ICoreClientAPI capi, ItemSlot mapSlot, ModSystemDrawableMap mapModSys) : base(capi)
		{
			this.mapSlot = mapSlot;
			this.mapModSys = mapModSys;
			LoadMapData();
			SetupDialog();
		}

		private void LoadMapData()
		{
			if (mapSlot?.Itemstack?.Attributes == null) return;

			// Загружаем масштаб карты из атрибута, если указан, иначе используем по умолчанию 32
			mapSize = mapSlot.Itemstack.Attributes.GetInt("mapSize", 32);
			cellSize = (int)((16 * 32) / mapSize);
			cellStates = new bool[mapSize, mapSize];

			// Загружаем название карты
			mapTitle = mapSlot.Itemstack.Attributes.GetString("title");
			if (string.IsNullOrEmpty(mapTitle))
			{
				mapTitle = mapSlot.Itemstack.GetName(); // Получаем стандартное название предмета
			}
			if (string.IsNullOrEmpty(mapTitle))
			{
				mapTitle = Lang.Get("craftablecartography:item-map");
			}

			if (mapSlot.Itemstack.Attributes.HasAttribute("mapData"))
			{
				byte[] mapData = mapSlot.Itemstack.Attributes.GetBytes("mapData");
				if (mapData != null && mapData.Length >= mapSize * mapSize / 8)
				{
					for (int x = 0; x < mapSize; x++)
					{
						for (int y = 0; y < mapSize; y++)
						{
							int index = y * mapSize + x;
							int byteIndex = index / 8;
							int bitIndex = index % 8;

							cellStates[x, y] = (mapData[byteIndex] & (1 << bitIndex)) != 0;
						}
					}
				}
			}
		}

		private byte[] ConvertToByteArray()
		{
			byte[] mapData = new byte[mapSize * mapSize / 8 + 1];
			for (int x = 0; x < mapSize; x++)
			{
				for (int y = 0; y < mapSize; y++)
				{
					int index = y * mapSize + x;
					int byteIndex = index / 8;
					int bitIndex = index % 8;

					if (cellStates[x, y])
					{
						mapData[byteIndex] |= (byte)(1 << bitIndex);
					}
				}
			}
			return mapData;
		}

		private void SaveMapData()
		{
			ICoreClientAPI api = capi;

			// Отладочная информация
			api.Logger.Debug($"Saving map data - Size: {mapSize}, Title: {mapTitle}");

			// Сохраняем через ModSystem для синхронизации
			byte[] mapData = ConvertToByteArray();

			// Проверяем, что mapData не null
			if (mapData == null)
			{
				api.Logger.Error("Map data is null!");
				return;
			}

			api.Logger.Debug($"Map data length: {mapData.Length}");

			try
			{
				mapModSys.EndEdit(capi.World.Player, mapData, mapTitle, mapSize);
				api.Logger.Debug("Map saved successfully with scale: " + mapSize);
			}
			catch (Exception ex)
			{
				api.Logger.Error("Failed to save map: " + ex.Message);
			}
		}

		private void SetupDialog()
		{
			ICoreClientAPI api = capi;

			// Размер сетки
			int gridWidth = mapSize * (cellSize + gridPadding) + gridPadding;
			int gridHeight = mapSize * (cellSize + gridPadding) + gridPadding;

			// ОБЩИЕ ГРАНИЦЫ ДИАЛОГА - определяют отступы от краев экрана
			ElementBounds dialogBounds = ElementBounds.Fixed(
				EnumDialogArea.CenterMiddle,    // Центрирование по середине экрана
				0, 0,                           // Смещение от центра (0,0)
				gridWidth + 40,                 // Ширина диалога
				gridHeight + 150                // Увеличиваем высоту диалога для выпадающего списка
			);

			// ФОН ДИАЛОГА - занимает всю область диалога
			ElementBounds bgBounds = ElementBounds.Fixed(0, 0, dialogBounds.fixedWidth, dialogBounds.fixedHeight);
			// ГРАНИЦЫ ПОЛЯ ВВОДА НАЗВАНИЯ - располагается под заголовком
			ElementBounds titleInputBounds = ElementBounds.Fixed(15, 1, dialogBounds.fixedWidth - 120, 30);
			// ГРАНИЦЫ ВЫПАДАЮЩЕГО СПИСКА - располагается под полем ввода названия
			ElementBounds dropdownText = ElementBounds.Fixed(20, 40, gridWidth - 80, 30);
			ElementBounds dropdownBounds = ElementBounds.Fixed(gridWidth - 80, 40, 100, 30);
			// ГРАНИЦЫ СЕТКИ - определяют позицию сетки внутри диалога
			ElementBounds gridBounds = ElementBounds.Fixed(20, 75, gridWidth, gridHeight); // Смещаем сетку ниже

			// Контейнер для кнопок - располагается под сеткой
			ElementBounds buttonContainer = ElementBounds.FixedSize(0, 0)
				.FixedUnder(gridBounds, 20.0) // ← 10px отступа от сетки
				.WithFixedWidth(dialogBounds.fixedWidth);

			// Кнопка "Очистить" - выровнена по ЛЕВОМУ краю
			ElementBounds clearButtonBounds = ElementBounds.FixedSize(0, 0)
				.WithAlignment(EnumDialogArea.LeftFixed)
				.WithFixedPadding(20.0, 4.0)
				.WithFixedAlignmentOffset(20.0, 0.0); // Отступ от левого края

			// Кнопка "Сохранить" - выровнена по ПРАВОМУ краю
			ElementBounds saveButtonBounds = ElementBounds.FixedSize(0, 0)
				.WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedPadding(20.0, 4.0)
				.WithFixedAlignmentOffset(-20.0, 0.0); // Отступ от правого края

			cellGrid = new GuiElementCellGrid(api, gridBounds, OnCellClicked, mapSize, cellStates, cellSize, gridPadding);

			// Определяем выбранный индекс по умолчанию
			int selectedIndex = 1; // 32 по умолчанию
			if (mapSize == 16) selectedIndex = 0;
			else if (mapSize == 64) selectedIndex = 2;

			SingleComposer = api.Gui
				.CreateCompo("drawmapgui", dialogBounds)
				.AddShadedDialogBG(bgBounds, true)
				.AddDialogTitleBar("", OnTitleBarClose)
				.AddTextInput(titleInputBounds, OnTitleChanged, null, "titleinput")
				.AddStaticText(Lang.Get("craftablecartography:size-map"), CairoFont.WhiteSmallishText(), dropdownText)
				.AddDropDown(
					new string[] { "16", "32", "64" },
					new string[] { "16x16", "32x32", "64x64" },
					selectedIndex,
					OnMapSizeChanged,
					dropdownBounds,
					"mapsizedropdown"
				)
				.AddInteractiveElement(cellGrid, "cellgrid")
				.BeginChildElements(buttonContainer)
					.AddSmallButton(Lang.Get("craftablecartography:clear-map"), OnClearClicked, clearButtonBounds)
					.AddSmallButton(Lang.Get("craftablecartography:save-map"), OnSaveClicked, saveButtonBounds)
				.EndChildElements()
				.Compose();

			// Устанавливаем начальное значение названия
			var titleInput = SingleComposer.GetTextInput("titleinput");
			titleInput.SetValue(mapTitle);
		}

		private void OnTitleChanged(string newTitle)
		{
			mapTitle = newTitle;
		}

		private void OnMapSizeChanged(string code, bool selected)
		{
			if (selected)
			{
				int newSize = int.Parse(code);
				if (newSize != mapSize)
				{
					mapSize = newSize;
					cellSize = (int)((16 * 32) / mapSize);
					cellStates = new bool[mapSize, mapSize];

					// Перестраиваем диалог с новыми параметрами
					SingleComposer?.Dispose();
					SetupDialog();
				}
			}
		}

		private void OnCellClicked(int x, int y)
		{
			ICoreClientAPI api = capi;

			// Запоминаем цвет, который устанавливаем при первом клике
			if (!isMouseDown || !lastPaintColor.HasValue)
			{
				lastPaintColor = !cellStates[x, y]; // Запоминаем противоположный цвет (тот, который будем устанавливать)
			}

			// Меняем цвет только если текущий цвет клетки отличается от запомненного
			if (cellStates[x, y] != lastPaintColor.Value)
			{
				cellStates[x, y] = lastPaintColor.Value;
				cellGrid.MarkDirty();
			}
		}

		public override void OnMouseDown(MouseEvent args)
		{
			base.OnMouseDown(args);
			if (args.Button == EnumMouseButton.Left)
			{
				isMouseDown = true;
				lastPaintColor = null; // Сбрасываем запомненный цвет при новом нажатии
			}
		}

		public override void OnMouseUp(MouseEvent args)
		{
			base.OnMouseUp(args);
			if (args.Button == EnumMouseButton.Left)
			{
				isMouseDown = false;
				lastPaintColor = null; // Сбрасываем запомненный цвет при отпускании
			}
		}

		public override void OnMouseMove(MouseEvent args)
		{
			base.OnMouseMove(args);
			if (isMouseDown && cellGrid.IsPositionInside(args.X, args.Y))
			{
				cellGrid.HandleDrag(args.X, args.Y);
			}
			else
			{
				// Обновляем позицию курсора для отображения границ
				cellGrid.UpdateHoverPosition(args.X, args.Y);
			}
		}

		private bool OnClearClicked()
		{
			for (int x = 0; x < mapSize; x++)
			{
				for (int y = 0; y < mapSize; y++)
				{
					cellStates[x, y] = false;
				}
			}
			cellGrid.MarkDirty();
			lastPaintColor = null; // Сбрасываем запомненный цвет при очистке
			return true;
		}

		private bool OnSaveClicked()
		{
			ICoreClientAPI api = capi;

			SaveMapData();
			TryClose();
			return true;
		}

		private void OnTitleBarClose()
		{
			// При закрытии через крестик отменяем редактирование
			mapModSys.CancelEdit(capi.World.Player);
			TryClose();
		}

		public override bool PrefersUngrabbedMouse => true;

		public override void OnGuiClosed()
		{
			base.OnGuiClosed();
			// Гарантируем, что сессия редактирования завершена
			mapModSys.CancelEdit(capi.World.Player);
		}
	}

	public class GuiElementCellGrid : GuiElement
	{
		private int mapSize;
		private bool[,] cellStates;
		private int cellSize;
		private int gridPadding;
		private Action<int, int> onCellClicked;
		private LoadedTexture solidTex;
		private int lastCellX = -1;
		private int lastCellY = -1;
		private int hoverCellX = -1;
		private int hoverCellY = -1;

		public GuiElementCellGrid(ICoreClientAPI capi, ElementBounds bounds, Action<int, int> onCellClicked, int mapSize, bool[,] cellStates, int cellSize, int gridPadding)
			: base(capi, bounds)
		{
			this.mapSize = mapSize;
			this.onCellClicked = onCellClicked;
			this.cellStates = cellStates;
			this.cellSize = cellSize;
			this.gridPadding = gridPadding;
			solidTex = new LoadedTexture(capi, 0, 1, 1);
			int[] whitePixel = new int[] { ColorUtil.ToRgba(255, 255, 255, 255) };
			capi.Render.LoadOrUpdateTextureFromRgba(whitePixel, false, 0, ref solidTex);
		}

		public override void RenderInteractiveElements(float deltaTime)
		{
			float guiScaleValue = ClientSettings.GUIScale;
			int cellSize1 = (int)(cellSize * guiScaleValue); // Учет масштаба GUI

			base.RenderInteractiveElements(deltaTime);

			int renderX = (int)Bounds.renderX; // Начальная позиция X
			int renderY = (int)Bounds.renderY; // Начальная позиция Y

			for (int x = 0; x < mapSize; x++)
			{
				for (int y = 0; y < mapSize; y++)
				{
					// Расчет позиции каждой клетки
					int posX = renderX + x * (cellSize1 + gridPadding);
					int posY = renderY + y * (cellSize1 + gridPadding);

					// Выбор цвета: черный для true, белый для false
					Vec4f tint = cellStates[x, y] ? new Vec4f(0f, 0f, 0f, 0.3f) : new Vec4f(1f, 1f, 1f, 0.3f);

					api.Render.Render2DTexture(solidTex.TextureId, posX, posY, cellSize1, cellSize1, 50, tint);

					// Рисуем границу только для клетки под курсором
					if (x == hoverCellX && y == hoverCellY)
					{
						api.Render.RenderRectangle(posX, posY, 60, cellSize1, cellSize1, ColorUtil.ToRgba(255, 0, 0, 0));
					}
				}
			}
		}

		public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
		{
			base.OnMouseDownOnElement(api, args);

			HandleMouseClick(args);
		}

		public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
		{
			base.OnMouseMove(api, args);

			if (args.Button == (int)EnumMouseButton.Left)
			{
				HandleMouseClick(args);
			}

			// Обновляем позицию курсора
			UpdateHoverPosition(args.X, args.Y);
		}

		private void HandleMouseClick(MouseEvent args)
		{
			float guiScaleValue = ClientSettings.GUIScale;
			int cellSize1 = (int)(cellSize * guiScaleValue);

			double relativeX = args.X - Bounds.renderX;
			double relativeY = args.Y - Bounds.renderY;

			int cellX = (int)(relativeX / (cellSize1 + gridPadding));
			int cellY = (int)(relativeY / (cellSize1 + gridPadding));

			if (cellX >= 0 && cellX < mapSize && cellY >= 0 && cellY < mapSize)
			{
				onCellClicked?.Invoke(cellX, cellY);
				lastCellX = cellX;
				lastCellY = cellY;
			}
		}

		public void HandleDrag(double mouseX, double mouseY)
		{
			float guiScaleValue = ClientSettings.GUIScale;
			int cellSize1 = (int)(cellSize * guiScaleValue);

			double relativeX = mouseX - Bounds.renderX;
			double relativeY = mouseY - Bounds.renderY;

			int cellX = (int)(relativeX / (cellSize1 + gridPadding));
			int cellY = (int)(relativeY / (cellSize1 + gridPadding));

			if (cellX >= 0 && cellX < mapSize && cellY >= 0 && cellY < mapSize)
			{
				// Если координаты изменились, обрабатываем новую клетку
				if (cellX != lastCellX || cellY != lastCellY)
				{
					onCellClicked?.Invoke(cellX, cellY);
					lastCellX = cellX;
					lastCellY = cellY;
				}
			}
		}

		public void UpdateHoverPosition(double mouseX, double mouseY)
		{
			float guiScaleValue = ClientSettings.GUIScale;
			int cellSize1 = (int)(cellSize * guiScaleValue);

			double relativeX = mouseX - Bounds.renderX;
			double relativeY = mouseY - Bounds.renderY;

			int newHoverCellX = (int)(relativeX / (cellSize1 + gridPadding));
			int newHoverCellY = (int)(relativeY / (cellSize1 + gridPadding));

			// Проверяем границы и обновляем только если позиция изменилась
			if (newHoverCellX >= 0 && newHoverCellX < mapSize &&
				newHoverCellY >= 0 && newHoverCellY < mapSize)
			{
				if (newHoverCellX != hoverCellX || newHoverCellY != hoverCellY)
				{
					hoverCellX = newHoverCellX;
					hoverCellY = newHoverCellY;
					MarkDirty(); // Перерисовываем для обновления границ
				}
			}
			else if (hoverCellX != -1 || hoverCellY != -1)
			{
				// Курсор вышел за пределы сетки - сбрасываем позицию
				hoverCellX = -1;
				hoverCellY = -1;
				MarkDirty(); // Перерисовываем для скрытия границ
			}
		}

		public bool IsPositionInside(double x, double y)
		{
			return x >= Bounds.renderX && x <= Bounds.renderX + Bounds.OuterWidth &&
				   y >= Bounds.renderY && y <= Bounds.renderY + Bounds.OuterHeight;
		}

		public void MarkDirty()
		{
			// Помечаем элемент как нуждающийся в перерисовке
			Bounds.CalcWorldBounds();
		}

		public override void Dispose()
		{
			base.Dispose();
			solidTex?.Dispose();
		}
	}
}