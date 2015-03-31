using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DebugLib
{
	/// <summary>
	/// 例外検証を行うクラス。
	/// </summary>
	public static class AssertEx
	{
		/* コレクションのアサーションは Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert で可能 */

		/// <summary>
		/// 指定したactionが指定した例外をスローすることを検証する。
		/// actionがnullの場合、例外をスローしない場合、Tと異なる例外を
		/// スローした場合はAssertFailedExceptionが発生する。
		/// </summary>
		/// <typeparam name="T">ExceptionまたはExceptionの派生型</typeparam>
		/// <param name="action">検証対象のaction</param>
		/// <param name="allowDerivedTypes">Tを派生した例外を検証対象とするか。</param>
		/// <param name="message">検証が失敗したときのメッセージ。</param>
		public static void Throws<T>(Action action, bool allowDerivedTypes = false, string message = "")
			where T : Exception
		{
			if (action == null) throw new AssertFailedException("actionにnullを指定することはできません。" + message);

			try
			{
				action();
			}
			catch (Exception ex)
			{
				if (ex.GetType() == typeof(T)) return;
				if (allowDerivedTypes && ex is T) return;

				throw new AssertFailedException(string.Format(
					"actionは期待された例外 {0} とは異なる例外 {1} をスローしました。{2}",
					typeof(T), ex.GetType(), message));
			}

			throw new AssertFailedException("actionは例外をスローしません。" + message);
		}
	}
}
