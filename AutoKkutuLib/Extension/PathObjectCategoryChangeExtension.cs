using AutoKkutuLib.Database;

namespace AutoKkutuLib.Extension;

/// <summary>
/// <c>PathObject</c>의 단어 종류를 바꾸는 기능을 지원하는 확장 클래스입니다.
/// </summary>
public static class PathObjectCategoryChangeExtension
{
	public static void MakeAttack(this DbConnectionBase connection, PathObject pathObject, GameMode mode)
	{
		if (pathObject.Categories.HasFlag(WordCategories.AttackWord))
			return;

		var node = pathObject.ToNode(mode);
		connection.Query.DeleteNode(mode.GetEndWordListTableName()).Execute(node);
		if (connection.Query.AddNode(mode.GetAttackWordListTableName()).Execute(node))
			LibLogger.Info(nameof(PathObjectCategoryChangeExtension), I18n.PathMark_Success, node, I18n.PathMark_Attack, mode);
		else
			LibLogger.Warn(nameof(PathObjectCategoryChangeExtension), I18n.PathMark_AlreadyDone, node, I18n.PathMark_Attack, mode);
	}

	public static void MakeEnd(this DbConnectionBase connection, PathObject pathObject, GameMode mode)
	{
		if (pathObject.Categories.HasFlag(WordCategories.EndWord))
			return;

		var node = pathObject.ToNode(mode);
		connection.Query.DeleteNode(mode.GetAttackWordListTableName()).Execute(node);
		if (connection.Query.AddNode(mode.GetEndWordListTableName()).Execute(node))
			LibLogger.Info(nameof(PathObjectCategoryChangeExtension), I18n.PathMark_Success, node, I18n.PathMark_End, mode);
		else
			LibLogger.Warn(nameof(PathObjectCategoryChangeExtension), I18n.PathMark_AlreadyDone, node, I18n.PathMark_End, mode);
	}

	public static void MakeNormal(this DbConnectionBase connection, PathObject pathObject, GameMode mode)
	{
		if (!pathObject.Categories.HasFlag(WordCategories.EndWord) && !pathObject.Categories.HasFlag(WordCategories.AttackWord))
			return;

		var node = pathObject.ToNode(mode);
		var endWord = connection.Query.DeleteNode(mode.GetEndWordListTableName()).Execute(node) > 0;
		var attackWord = connection.Query.DeleteNode(mode.GetAttackWordListTableName()).Execute(node) > 0;
		if (endWord || attackWord)
			LibLogger.Info(nameof(PathObjectCategoryChangeExtension), I18n.PathMark_Success, node, I18n.PathMark_Normal, mode);
		else
			LibLogger.Warn(nameof(PathObjectCategoryChangeExtension), I18n.PathMark_AlreadyDone, node, I18n.PathMark_Normal, mode);
	}

	// TODO: Move to other class
	public static string GetAttackWordListTableName(this GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => DatabaseConstants.ReverseAttackNodeIndexTableName,
		GameMode.Kkutu => DatabaseConstants.KkutuAttackNodeIndexTableName,
		GameMode.KungKungTta => DatabaseConstants.KKTAttackNodeIndexTableName,
		_ => DatabaseConstants.AttackNodeIndexTableName,
	};

	// TODO: Move to other class
	public static string GetEndWordListTableName(this GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => DatabaseConstants.ReverseEndNodeIndexTableName,
		GameMode.Kkutu => DatabaseConstants.KkutuEndNodeIndexTableName,
		GameMode.KungKungTta => DatabaseConstants.KKTEndNodeIndexTableName,
		_ => DatabaseConstants.EndNodeIndexTableName,
	};

	private static string ToNode(this PathObject pathObject, GameMode mode)
	{
		var content = pathObject.Content;
		switch (mode)
		{
			case GameMode.FirstAndLast:
				return content.GetFaLTailNode();

			case GameMode.MiddleAndFirst:
				if (content.Length % 2 == 1)
					return content.GetMaFTailNode();
				break;

			case GameMode.Kkutu:
				if (content.Length > 2)
					return content.GetKkutuTailNode();
				break;
		}

		return content.GetLaFTailNode();
	}
}
