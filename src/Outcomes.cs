/*
Vectoray; Home-brew 3D C# game engine.
Copyright (C) 2020 LukeZaz

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;

namespace Vectoray
{
    // TODO: Unit testing.
    /// <summary>
    /// An exception that is thrown when an `Opt&lt;T&gt;` is unwrapped, but its value is `null` or `None`.
    /// </summary>
    public class EmptyUnwrapException : Exception
    {
        public EmptyUnwrapException() : base() { }
        public EmptyUnwrapException(string message) : base(message) { }
        public EmptyUnwrapException(string message, Exception inner) : base(message, inner) { }
    }

    // TODO: Investigate what's giving the formatting options for the XML summaries.
    // I thought it was Rust, but I disabled it for this workspace and it still worked so...?
    /// <summary>
    /// Used to wrap a value that may be null such that it cannot be used until properly checked,
    /// thereby avoiding NullReference exceptions.
    /// 
    /// Various methods exist to retrieve the inner value, including `Unwrap` and `UnwrapOr(T defaultVal)`,
    /// however the recommended method is to use pattern matching, e.g.:
    /// ```
    /// Opt&lt;int&gt; example = new Some&lt;int&gt;(5);
    /// if (example is Some&lt;int&gt;(int inner))
    /// Console.WriteLine(inner); // Prints '5'.
    /// ```
    /// </summary>
    /// <typeparam name="T">The wrapped type of this Option.</typeparam>
    #region Explanation for abstract class
    // While Some<T> and None<T> are both perfect fits for being structs, and thus Opt<T> would be fine as an interface,
    // there are two issues that prevent this from being ideal:

    // A) A struct cannot have a custom parameterless constructor. This means the default parameterless
    //    constructor cannot be made inaccessible, making it all too easy to create an invalid Some<T> with it.

    //    It's true you can do this regardless using by using arrays, but the goal is not to make it
    //    impossible - only hard enough that doing so makes you think, "Should I *really* be doing this?"

    // B) Interfaces can only have public accessibility. Due to this, it's not possible to prevent classes
    //    from outside this assembly from inheriting it. It should be a guarantee that anything that
    //    returns Opt<T> is either Some<T> or None<T>; allowing other classes to extend Opt<T> ruins
    //    this assumption.

    // A regular class that simply uses pattern matching to figure out what it is and return accordingly would
    // also work, however I'm unaware of any way to customize the XML docs for the subclasses in this scenario.
    // Additionally, seeing as the XML docs comprise probably about 85% of the contents of this file, compacting the
    // code into the base class would save nothing unless it first eliminated a huge amount of said documentation.
    #endregion
    public abstract class Opt<T>
    {
        #region Explanation for access modifier
        // Private protected limits access to the containing class or any that derives from it
        // within the same assembly. Since classes cannot inherit from a class whose constructors it does
        // not have access to, this means that inheriting Opt<T> is limited *and* that users of Opt derived classes
        // are forced to use their constructors only.

        // The reason for this is that Opt should only be inherited by two classes: Some and None,
        // so as to ensure it and its derivatives form a simple and predictable structure.
        // If a function returns Opt<T>, you should be able to assume it's either Some or None;
        // an unexpected third class would only introduce confusion.
        #endregion
        private protected Opt() { }

        /// <summary>
        /// Attempt to unwrap this option and retrieve the value inside.
        /// 
        /// It's not recommended to use this method
        /// unless a value of `None` constitutes an extraordinary error; instead, use pattern
        /// matching via `is`. See the documentation for `Opt&lt;T&gt;` for an example.
        /// 
        /// Will throw an `EmptyUnwrapException` if this option is either `None` or has an inner value of `null`.
        /// </summary>
        /// <returns>
        /// The value encapsulated by this option, provided this option represents an instance of `Some&lt;T&gt;`.
        /// </returns>
        /// <exception cref="EmptyUnwrapException">
        /// Thrown if this option is either a `None` class or has an inner value of `null`.
        /// </exception>
        public abstract T Unwrap();

        /// <summary>
        /// Attempt to unwrap this option and retrieve the value inside, or return `defaultVal`
        /// if this option is either `None` or `null`.
        /// 
        /// **Example:**
        /// ```
        /// Opt&lt;int&gt; empty = new None&lt;int&gt;();
        /// int val = empty.UnwrapOr(5);
        /// Console.WriteLine(val); // Prints '5'.
        /// ```
        /// </summary>
        /// <param name="defaultVal">The value to default to if this option is either `None` or `null`.</param>
        /// <returns>The inner value of this option, or `defaultVal` if this option is either `None` or `null`.</returns>
        public abstract T UnwrapOr(T defaultVal);

        /// <summary>
        /// Attempt to unwrap this option and retrieve the value inside. If `None` or `null` is encountered,
        /// this will use `message` to throw an `EmptyUnwrapException`.
        /// 
        /// It's not recommended to use this method
        /// unless a value of `None` constitutes an extraordinary error; instead, use pattern
        /// matching via `is`. See the documentation for `Opt&lt;T&gt;` for an example.
        /// </summary>
        /// <param name="message">The message to pass to any `EmptyUnwrapException`s thrown by this method.</param>
        /// <returns>
        /// The value encapsulated by this option, provided this option represents an instance of `Some&lt;T&gt;`.
        /// </returns>
        /// <exception cref="EmptyUnwrapException">
        /// Thrown if this option is either a `None` class or has an inner value of `null`.
        /// </exception>
        public abstract T Expect(string message);

        /// <summary>
        /// Map this `Opt&lt;T&gt;` to `Opt&lt;R&gt;` by using the provided function
        /// to convert the inner value.
        /// </summary>
        /// <param name="mapFunction">The function to use to map the inner value.</param>
        /// <typeparam name="R">The new `Opt` type to return.</typeparam>
        /// <returns>The converted `Opt&lt;R&gt;` value.</returns>
        public abstract Opt<R> Map<R>(Func<T, R> mapFunction);

        /// <summary>
        /// Deconstruct this `Opt`. Effectively an alias for `Unwrap()` that instead utilizes an `out` argument.
        /// Primarily provided for pattern matching purposes.
        /// </summary>
        /// <param name="value">The variable to fill with the inner value of this `Opt`.</param>
        /// <exception cref="EmptyUnwrapException">
        /// Thrown if this option is either a `None` class or has an inner value of `null`.
        /// </exception>
        public void Deconstruct(out T value) => value = Unwrap();
    }

    /// <summary>
    /// A container deriving `Opt&lt;T&gt;`, used to represent an option that has a usable inner value.
    /// 
    /// Various methods exist to retrieve the inner value, including `Unwrap` and `UnwrapOr(T defaultVal)`,
    /// however the recommended method is to use pattern matching, e.g.:
    /// ```
    /// Opt&lt;int&gt; example = new Some&lt;int&gt;(5);
    /// if (example is Some&lt;int&gt;(int inner))
    /// Console.WriteLine(inner); // Prints '5'.
    /// ```
    /// </summary>
    /// <typeparam name="T">The type of the inner value of this `Some`.</typeparam>
    public sealed class Some<T> : Opt<T>
    {
        private readonly T value;

        /// <summary>
        /// Create a new `Some` to wrap `value`.
        /// </summary>
        /// <param name="value">The value to wrap in this `Some`. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if `value` is null.</exception>
        public Some(T value)
        {
            if (value == null) throw new ArgumentNullException("Cannot create a Some<T> with a null value.");
            else this.value = value;
        }

        public static implicit operator Some<T>(T value) => new Some<T>(value);

        /// <summary>
        /// Unwrap this `Some` and retrieve the value inside.
        /// </summary>
        /// <returns>The value encapsulated by this `Some`.</returns>
        /// <exception cref="EmptyUnwrapException">
        /// Can be thrown in the extraordinary event an instance of `Some` is somehow created with a null inner value.
        /// </exception>
        public override T Unwrap() => Expect("Attempted to call Opt<T>.Unwrap on a Some<T> with a value of null. (...how?)");

        /// <summary>
        /// Unwrap this `Some` and retrieve the value inside. While other options use `defaultVal`
        /// as a backup value, null values are not allowed for instances of `Some`; hence, `defaultVal`
        /// will never be returned.
        /// 
        /// For obvious reasons, you probably don't want to use this function.
        /// Instead, use either pattern matching or `Unwrap()`.
        /// </summary>
        /// <param name="defaultVal">The value that would be defaulted to, were this an instance of `None` instead.
        /// Never used.</param>
        /// <returns>The value encapsulated by this `Some`.</returns>
        /// <exception cref="EmptyUnwrapException">
        /// Can be thrown in the extraordinary event an instance of `Some` is somehow created with a null inner value.
        /// This should never happen and represents a serious error, so this exception is used instead of `defaultVal`.
        /// </exception>
        public override T UnwrapOr(T defaultVal) =>
        Expect("Attempted to call Opt<T>.UnwrapOr on a Some<T> with a value of null. (...how?)");

        /// <summary>
        /// Unwrap this `Some` and retrieve the value inside.
        /// In the extraordinary event this fails, this will use `message` to throw a custom `EmptyUnwrapException`.
        /// </summary>
        /// <returns>The value encapsulated by this `Some`.</returns>
        /// <exception cref="EmptyUnwrapException">
        /// Can be thrown in the extraordinary event an instance of `Some` is somehow created with a null inner value.
        /// If this happens, `message` will be used to create the exception.
        /// </exception>
        public override T Expect(string message)
        {
            // This value should really, *really* never be null, but it's not impossible, so check anyway.
            if (value == null) throw new EmptyUnwrapException(message);
            else return value;
        }

        /// <summary>
        /// Map this `Some&lt;T&gt;` to `Some&lt;R&gt;` by using the provided function
        /// to convert the inner value.
        /// </summary>
        /// <param name="mapFunction">The function to use to map the inner value.</param>
        /// <typeparam name="R">The new `Some` type to return.</typeparam>
        /// <returns>The converted `Some&lt;R&gt;` value.</returns>
        public override Opt<R> Map<R>(Func<T, R> mapFunction) => new Some<R>(mapFunction(value));
    }

    /// <summary>
    /// A container deriving `Opt&lt;T&gt;`, used to represent an option that has no inner value.
    /// 
    /// Various methods exist to check for this type, including `Unwrap` and `UnwrapOr(T defaultVal)`,
    /// however the recommended method is to use pattern matching, e.g.:
    /// ```
    /// Opt&lt;int&gt; example = new None&lt;int&gt;();
    /// if (example is None&lt;int&gt;)
    /// Console.WriteLine("'Example' was an empty option.");
    /// ```
    /// </summary>
    /// <typeparam name="T">The type of what would be the inner value of this `None`, were it instead a `Some`.</typeparam>
    public sealed class None<T> : Opt<T>
    {
        /// <summary>
        /// Create a new `None` value.
        /// </summary>
        public None() { }

        /// <summary>
        /// Attempt to unwrap an inner value from this `None`. As this does not have
        /// an inner value, this **will** throw an EmptyUnwrapException.
        /// 
        /// For obvious reasons, you probably don't want to use this method.
        /// </summary>
        /// <returns>Nothing; this method is guaranteed to throw an EmptyUnwrapException.</returns>
        /// <exception cref="EmptyUnwrapException">Always thrown.</exception>
        public override T Unwrap() => Expect("Attempted to call Opt<T>.Unwrap on a None value.");

        /// <summary>
        /// Attempt to unwrap an inner value from this `None`, using `defaultVal` as a backup value. As this does
        /// not have an inner value, this will **always** return `defaultVal`.
        /// 
        /// For obvious reasons, you probably don't want to use this method.
        /// </summary>
        /// <param name="defaultVal">The value to return, as this class cannot be successfully unwrapped.</param>
        /// <returns>Always returns `defaultVal`.</returns>
        public override T UnwrapOr(T defaultVal) => defaultVal;

        /// <summary>
        /// Attempt to unwrap an inner value from this `None`. As this does not have
        /// an inner value, this **will** throw an EmptyUnwrapException using `message` as its message.
        /// 
        /// For obvious reasons, you probably don't want to use this method.
        /// </summary>
        /// <param name="message">The message to pass to the `EmptyUnwrapException` thrown by this method.</param>
        /// <returns>Nothing; this method is guaranteed to throw an EmptyUnwrapException.</returns>
        /// <exception cref="EmptyUnwrapException">Always thrown.</exception>
        public override T Expect(string message) =>
        throw new EmptyUnwrapException(message);

        /// <summary>
        /// Map this `None&lt;T&gt;` to `None&lt;R&gt;` by using the provided function
        /// to convert the inner value.
        /// </summary>
        /// <param name="mapFunction">The function to use to map the inner value.</param>
        /// <typeparam name="R">The new `None` type to return.</typeparam>
        /// <returns>The converted `None&lt;R&gt;` value.</returns>
        public override Opt<R> Map<R>(Func<T, R> mapFunction) => new None<R>();
    }

    // Why yes, I did program in Rust for a while! Why do you ask?
}