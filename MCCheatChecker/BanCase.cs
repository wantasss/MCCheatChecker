namespace MCCheatChecker
{
	public enum BanCase
	{
		// Официальный регламент Раздела 5
		CheatClients,               // 5.01. Чит-клиенты (30d / perm)
		CombatAdvantage,            // 5.02. Преимущество в бою (30d)
		Automation,                 // 5.03. Автоматизация действий (15-30d)
		MovementAndBlocks,          // 5.04. Передвижение и работа с блоками (30d)
		ExtraInfoAndVision,         // 5.05. Лишняя информация и обзор (15d)
		BaseAndOreSearch,           // 5.06. Поиск баз, руды и сида (30d)
		InvisibleBlocksAndEffects,  // 5.07. Невидимые блоки и эффекты (15-30d)
		Important,                  // 5.08. Важно (Аналоги модов 15-30d / perm)

		// Legacy aliases for full backward compatibility
		CheatUsage,
		CheatStorage,
		CheatTraces,
		Visuals,
		Macro,
		Confession,
		RefuseCheck,
		ObstructCheck,
		ServicesDisabled,
		NetworkRegistry,
		OldWindows,
		BugAbuse
	}
}
