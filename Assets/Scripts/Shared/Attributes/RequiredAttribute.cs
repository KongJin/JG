using System;
using UnityEngine;

namespace Shared.Attributes
{
    /// <summary>
    /// SerializeField에 붙이면 씬/프리팹 저장 시 null 검증 대상이 된다.
    /// Editor의 RequiredFieldValidator가 검사하고, PropertyDrawer가 Inspector에 빨간 표시를 한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RequiredAttribute : PropertyAttribute { }
}
