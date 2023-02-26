using AutoKkutuLib.Database;
using Serilog;

namespace AutoKkutuLib.Extension;
public static class PathObjectCategoryChangeExtension
{
	public static void MakeAttack(this AbstractDatabaseConnection connection, PathObject pathObject, GameMode mode)
	{
		if (pathObject.Categories.HasFlag(WordCategories.AttackWord))
			return;

		var node = pathObject.ToNode(mode);
		connection.Query.DeleteNode(GetEndWordListTableName(mode)).Execute(node);
		if (connection.Query.AddNode(GetAttackWordListTableName(mode)).Execute(node))
			Log.Information(I18n.PathMark_Success, node, I18n.PathMark_Attack, mode);
		else
			Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_Attack, mode);
	}

	public static void MakeEnd(this AbstractDatabaseConnection connection, PathObject pathObject, GameMode mode)
	{
		if (pathObject.Categories.HasFlag(WordCategories.EndWord))
			return;

		var node = pathObject.ToNode(mode);
		connection.Query.DeleteNode(GetAttackWordListTableName(mode)).Execute(node);
		if (connection.Query.AddNode(GetEndWordListTableName(mode)).Execute(node))
			Log.Information(I18n.PathMark_Success, node, I18n.PathMark_End, mode);
		else
			Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_End, mode);
	}

	public static void MakeNormal(this AbstractDatabaseConnection connection, PathObject pathObject, GameMode mode)
	{
		if (!pathObject.Categories.HasFlag(WordCategories.EndWord) && !pathObject.Categories.HasFlag(WordCategories.AttackWord))
			return;

		var node = pathObject.ToNode(mode);
		var endWord = connection.Query.DeleteNode(GetEndWordListTableName(mode)).Execute(node) > 0;
		var attackWord = connection.Query.DeleteNode(GetAttackWordListTableName(mode)).Execute(node) > 0;
		if (endWord || attackWord)
			Log.Information(I18n.PathMark_Success, node, I18n.PathMark_Normal, mode);
		else
			Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_Normal, mode);
	}

	private static string GetAttackWordListTableName(GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => DatabaseConstants.ReverseAttackNodeIndexTableName,
		GameMode.Kkutu => DatabaseConstants.KkutuAttackNodeIndexTableName,
		GameMode.KungKungTta => DatabaseConstants.KKTAttackNodeIndexTableName,
		_ => DatabaseConstants.AttackNodeIndexTableName,
	};

	private static string GetEndWordListTableName(GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => DatabaseConstants.ReverseEndNodeIndexTableName,
		GameMode.Kkutu => DatabaseConstants.KkutuEndNodeIndexTableName,
		GameMode.KungKungTta => DatabaseConstants.KKTEndNodeIndexTableName,
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
