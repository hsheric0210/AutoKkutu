using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Query.Relational;
using Serilog;

namespace AutoKkutuLib.Extension;
public static class PathObjectCategoryChangeExtension
{
	public static void MakeAttack(this AbstractDatabaseConnection connection, PathObject pathObject, GameMode mode)
	{
		if (pathObject.Categories.HasFlag(WordCategories.AttackWord))
			return;

		var node = pathObject.ToNode(mode);
		connection.DeleteNode(node, GetEndWordListTableName(mode));
		if (connection.AddNode(node, GetAttackWordListTableName(mode)))
			Log.Information(I18n.PathMark_Success, node, I18n.PathMark_Attack, mode);
		else
			Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_Attack, mode);
	}

	public static void MakeEnd(this AbstractDatabaseConnection connection, PathObject pathObject, GameMode mode)
	{
		if (pathObject.Categories.HasFlag(WordCategories.EndWord))
			return;

		var node = pathObject.ToNode(mode);
		connection.DeleteNode(node, GetAttackWordListTableName(mode));
		if (connection.AddNode(node, GetEndWordListTableName(mode)))
			Log.Information(I18n.PathMark_Success, node, I18n.PathMark_End, mode);
		else
			Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_End, mode);
	}

	public static void MakeNormal(this AbstractDatabaseConnection connection, PathObject pathObject, GameMode mode)
	{
		if (!pathObject.Categories.HasFlag(WordCategories.EndWord) && !pathObject.Categories.HasFlag(WordCategories.AttackWord))
			return;

		var node = pathObject.ToNode(mode);
		var endWord = connection.DeleteNode(node, GetEndWordListTableName(mode)) > 0;
		var attackWord = connection.DeleteNode(node, GetAttackWordListTableName(mode)) > 0;
		if (endWord || attackWord)
			Log.Information(I18n.PathMark_Success, node, I18n.PathMark_Normal, mode);
		else
			Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_Normal, mode);
	}

	private static string GetAttackWordListTableName(GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => DatabaseConstants.ReverseAttackNodeIndexTableName,
		GameMode.Kkutu => DatabaseConstants.KkutuAttackNodeIndexTableName,
		_ => DatabaseConstants.AttackNodeIndexTableName,
	};

	private static string GetEndWordListTableName(GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => DatabaseConstants.ReverseEndNodeIndexTableName,
		GameMode.Kkutu => DatabaseConstants.KkutuEndNodeIndexTableName,
		_ => DatabaseConstants.EndNodeIndexTableName,
	};

	private static string ToNode(this PathObject pathObject, GameMode mode)
	{
		string content = pathObject.Content;
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
