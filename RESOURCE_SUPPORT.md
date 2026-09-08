# Природные ресурсы

Добавлен раздел «Ресурсы»: поиск по списку, независимые галочки, дальность,
«Только жемчуг», снятие выбора. По умолчанию выбраны Silicon и BlackPearl.
Известные базовые виды доступны сразу; дополнительные классы добавляются из
HarvestResourceEntries/BaseHarvestResourceEntries при загрузке карты.
Одна залежь может соответствовать нескольким выбранным ресурсам.

## Источник данных

Смещения и типы извлечены через DIA из локального ShooterGame.pdb.
Сканер активен только для профиля SHA-256
9BC401417A776C5244A1B0B3255DC3AF4A9D73E3F5C1BA96228FBE3FB1A43477.

- AInstancedFoliageActor +0x488: TArray<UHierarchicalInstancedStaticMeshComponent*>.
- UInstancedStaticMeshComponent +0x708: PerInstanceSMData.
- FInstancedStaticMeshInstanceData: stride 0x50; FMatrix начинается с 0, translation +0x30.
- UInstancedStaticMeshComponent +0x748: RemovedInstances, TArray<int>.
- UInstancedStaticMeshComponent +0x760: AttachedComponentClass.
- UStaticMeshComponent +0x688: StaticMesh.
- USceneComponent +0xE0: ComponentToWorld, FTransform 0x30.
- UClass +0xF8: ClassDefaultObject.
- UPrimalHarvestingComponent +0xE8 / +0xF8: HarvestResourceEntries / BaseHarvestResourceEntries.
- FHarvestResourceEntry: stride 0x78, ResourceItem (UClass*) +0x18.

Имена ресурсов читаются из классов предметов harvesting-компонента, а не
выводятся из названий декоративных мешей. Проверяется наследование компонентов.
Координаты локального экземпляра преобразуются через ComponentToWorld.
Удалённые индексы и нулевые матрицы масштаба исключаются.

## Ограничения и проверка

Это реализация для живой проверки: компиляция и self-test пройдены, но игра
во время разработки не была запущена. Самопроверка не доказывает фактическое
заполнение массивов на сервере. Нужны проверки рядом с белым/чёрным жемчугом,
затем после его сбора и после телепорта/смены карты.

Читаются только загруженные foliage-контейнеры с harvesting-компонентами.
Ресурсы, реализованные модом отдельными акторами или иным контейнером,
этим сканером пока не покрываются. Луковицы/маршрут мутагена остаются отдельным
ранее существовавшим механизмом. Поэтому поддержка буквально всех ресурсов
на любых картах не заявляется.

Опрос постепенный: окно 64 компонентов при обнаружении, до 512 экземпляров
за один world capture. Полный список компонента публикуется после завершения
прохода, повторный проход начинается не раньше чем через секунду. При большом
числе компонентов задержка может быть выше. Вывод ограничен ближайшими 1500
точками. В arktracker.log раз в 15 секунд пишется Resources: foliage/components/points.
Если список не находит жемчуг, эти счётчики позволяют отличить отсутствие
контейнеров, harvesting-описаний и самих экземпляров.

Изменённые исходники: ResourceScanner.cs, ArkTracker.Legacy.cs,
WpfSettingsDashboard.cs. PdbFieldDump.cpp теперь принимает имена типов после
аргументов пути PDB и DIA DLL и выводит все поля выбранных типов.
