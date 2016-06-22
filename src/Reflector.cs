#region Copyright (c) 2016 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace DeJson
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    static class Reflector
    {
        public static ConstructorInfo Constructor<T>(Expression<Func<T>> e) =>
            ((NewExpression)e.Body).Constructor;

        public static MethodInfo Method<T>(Expression<Func<T>> e) =>
            ((MethodCallExpression)e.Body).Method;

        public static MethodInfo Method<T, TResult>(Expression<Func<T, TResult>> e) =>
            ((MethodCallExpression)e.Body).Method;

        public static MethodInfo VoidMethod<T>(Expression<Func<Action<T>>> e) =>
            ((MethodCallExpression)((LambdaExpression)e.Body).Body).Method;

        public static PropertyInfo Property<T, TResult>(Expression<Func<T, TResult>> e) =>
            (PropertyInfo)((MemberExpression)e.Body).Member;
    }
}