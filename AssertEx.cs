using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DebugLib
{
	/// <summary>
	/// 
	/// </summary>
	public static class AssertEx
	{
		/// <summary>
		/// 指定したactionが指定した例外をスローすることを検証する。
		/// actionがnullの場合、例外をスローしない場合、Tと異なる例外を
		/// スローした場合はAssertFailedExceptionが発生する。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		/// <param name="allowDerivedTypes">Tを派生した例外を検証対象とするか。</param>
		/// <param name="message">検証が失敗したときのメッセージ。</param>
		public static void Throws<T>(Action action, bool allowDerivedTypes = false, string message = "")
			where T : Exception
		{
			if (action == null)
				throw new AssertFailedException("actionにnullを指定することはできません。" + message);

			try
			{
				action();
			}
			catch (Exception ex)
			{
				if (ex.GetType() == typeof(T))
					return;
				if (allowDerivedTypes && ex is T)
					return;

				throw new AssertFailedException("actionは期待された例外 " + typeof(T) + " とは異なる例外 " + ex.GetType() + " をスローしました。" + message);
			}

			throw new AssertFailedException("actionは例外をスローしません。" + message);
		}
	}
}
