﻿using NUnit.Framework;
using Rubberduck.Inspections.Concrete;
using Rubberduck.Parsing.Inspections.Resources;
using RubberduckTests.Mocks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RubberduckTests.Inspections
{
    [TestFixture]
    public class UnreachableCaseInspectionTests
    {
        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SingleUnreachableCase()
        {
            const string inputCode =
@"Sub Foo()

Const x As String =""Bar""
Select Case x
  Case ""Foo"", ""Bar""
    'OK
  Case ""Food""
    'OK
  Case ""Bar""
    'Unreachable
  Case ""Foodie""
    'OK
End Select
                
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_RangeConflictsWithPriorIsStmt()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case Is > 5
    'OK
  Case 4 To 10
    'OK - overlap
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_PowOpEvaluationAlgebraNoDetection()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case x ^ 2 = 49
    'OK
  Case x = 7
    'Unreachable, but not detected - math/algebra on the Select Case variable yet to be supported
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_AlgebraNonInspectionFindsDuplicate()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case x ^ 2 = 49, x ^ 3 = 8
    'OK
  Case x = 7
    'Unreachable, but not detected - math/algebra on the Select Case variable yet to be supported
  Case x = 2
    'Unreachable, but not detected - math/algebra on the Select Case variable yet to be supported
  Case x ^ 2 = 49, x ^ 3 = 8
    'Unreacable - detected because it has identical range clause(s)
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_PowOpEvaluationAlgebra()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case x ^ 2 = 49
    'OK
  Case x = 7
    'Unreachable, but not detected - math/algebra on prior Select Case variable yet to be supported
  Case Is < 20
    'OK
  Case 13
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_TextOnlyCompareCopyPaste()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case Is > 500
    'OK
  Case x ^ 2 = 49, (CLng(VBA.Rnd() * 100) * x) < 30
    'OK
  Case 45 To 100
    'OK
  Case x ^ 2 = 49, (CLng(VBA.Rnd() * 100) * x) < 30
    'Unreachable - Copy/Paste
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_TextOnlyCompareCopyPasteOutofOrder()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case x ^ 2 = 49, (CLng(VBA.Rnd() * 100) * x) < 30
    'OK
  Case 45 To 100
    'OK
  Case (CLng(VBA.Rnd() * 100) * x) < 30, x ^ 2 = 49
    'Unreachable - Copy/Paste
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_InternalOverlap()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case Is > 5, 15, 20, Is < 55
    'OK
  Case 4 To 10
    'Unreachable
  Case True
    'Unreachable
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_MultiplyUsedSelectVariable()
        {
            const string inputCode =
@"Sub Foo(caseNum As Long)

Const x As String =""Bar""
Select Case x
  Case ""Foo"", ""Bar""
    'OK
  Case ""Foo""
    'Unreachable
  Case ""Bar""
    'Unreachable
  Case Else
    MsgBox ""Unable To handle "" & x 
End Select
                
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_MultipleUnreachableCases()
        {
            const string inputCode =
@"Sub Foo(x As String)

Select Case x
  Case ""Foo"", ""Bar""
    'OK
  Case ""Foo""
    'Unreachable
  Case ""Bar""
    'Unreachable
  Case ""Foo""
    'Unreachable
End Select
                
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 3);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CoverAllCases()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case x > -5000
    'OK
  Case Is < 5
    'OK
  Case 500 To 700
    'Unreachable
  Case Else
    'Unreachable
End Select
                
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CoverAllCasesSingleClause()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case  -5000, Is <> -5000
    'OK
  Case Is > 5
    'Unreachable
  Case 500 To 700
    'Unreachable
  Case Else
    'Unreachable
End Select
                
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseStmtCoversAll()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case 1 To 45, Is < 5, x > -5000
    'OK
  Case 5500
    'Unreachable
  Case 500 To 700
    'Unreachable
  Case Else
    'Unreachable
End Select
                
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseStmtCoversAllNEQ()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case x = -4, x <> -4
    'Only reachable case
  Case 5500
    'Unreachable
  Case 500 To 700
    'Unreachable
  Case Else
    'Unreachable
End Select
                
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_StringRange()
        {
            const string inputCode =
@"Sub Foo()

Const x As String =""Bar""
Select Case x
  Case ""Alpha"" To ""Omega""
    'OK
  Case ""Alphabet""
    'Unreachable
  Case ""Ohm""
    'Unreachable
  Case ""Omegaad""
    'OK
End Select
                
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_ReusedSelectExpressionVariable()
        {
            const string inputCode =
@"Sub Foo()

Const x As String =""Bar""
  
Select Case x
  Case ""Foo"", ""Bar""
    'OK
  Case ""Foo""
    'Unreachable
  Case ""Food""
        'OK
End Select
                
End Sub";

            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_NumberRange()
        {
            const string inputCode =
@"Sub Foo(x as Long)

Select Case x
  Case 1 To 100
    'OK
  Case 50
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }
        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_NumberRangeConstants()
        {
            const string inputCode =
@"Sub Foo(x as Long)

Const JAN As Long = 1
Const DEC As Long = 12
Const AUG As Long = 8

Select Case x
  Case JAN To DEC
    'OK
  Case AUG
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_NumberRangeMixedTypes()
        {
            const string inputCode =
@"Sub Foo(x as Long)

Select Case x
  Case 1 To ""Forever""
    'unreachable
  Case 1 To 50
    'OK
  Case 45
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1, mismatch: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_NumberRangeCummulativeCoverage()
        {
            const string inputCode =
@"Sub Foo(x as Long)

Select Case x
  Case 150 To 250
    'OK
  Case 1 To 100
    'OK
  Case 101 To 149
    'OK
  Case 25 To 249 
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_NumberRangeHighToLow()
        {
            const string inputCode =
@"Sub Foo(x as Long)

Select Case x
  Case 100 To 1
    'OK
  Case 50
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_NumberRangeInverted()
        {
            const string inputCode =
@"Sub Foo(x as Long)

Select Case x
  Case 100 To 1
    'OK
  Case 50
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseElseIsClausePlusRange()
        {
            const string inputCode =
@"Sub Foo(x as Long)

Select Case x
  Case Is > 200
    'OK
  Case 50 To 200
    'OK
  Case Is < 50
    'OK
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseElseIsClausePlusRangeAndSingles()
        {
            const string inputCode =
@"Sub Foo(x as Long)

Select Case x
  Case 53,54
    'OK
  Case Is > 200
    'OK
  Case 55 To 200
    'OK
  Case Is < 50
    'OK
  Case 50,51,52
    'OK
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EmbeddedSelectCase()
        {
            const string inputCode =
@"Sub Foo(x As Long, z As Long) 

Select Case x
  Case 1 To 10
    'OK
  Case 9
    'Unreachable
  Case 11
    Select Case  z
      Case 5 To 25
        'OK
      Case 6
        'Unreachable
      Case 8
        'Unreachable
      Case 15
        'Unreachable
    End Select
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 4);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EmbeddedSelectCaseUsingConstants()
        {
            const string inputCode =
@"Sub Foo()

Const x As String = ""Foo""
Const z As String = ""Bar""

Select Case x
  Case ""Foo"", ""Bar""
    'OK
  Case ""Foo""
    'Unreachable
  Case ""Food""
    Select Case  z
      Case ""Foo"", ""Bar"",""Goo""
        'OK
      Case ""Bar""
        'Unreachable
      Case ""Foo""
        'Unreachable
      Case ""Goo""
        'Unreachable
    End Select
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 4);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EmbeddedSelectCaseStringType()
        {
            const string inputCode =
@"Sub Foo(x As String, z As String)

'Const x As String = ""Foo""
'Const z As String = ""Bar""

Select Case x
  Case ""Foo"", ""Bar""
    'OK
  Case ""Foo""
    'Unreachable
  Case ""Food""
    Select Case  z
      Case ""Foo"", ""Bar"",""Goo""
        'OK
      Case ""Bar""
        'Unreachable
      Case ""Foo""
        'Unreachable
      Case ""Goo""
        'Unreachable
    End Select
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 4);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleLongCollision()
        {
            const string inputCode =
@"Sub Foo(x As Long)
Select Case x
  Case 1,2,-5
    'OK
  Case 2
    'Unreachable
  Case -5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleLongCollisionNegative()
        {
            const string inputCode =
@"Sub Foo(x As Long)
Select Case -x
  Case 1,2,-5
    'OK
  Case 2
    'Unreachable
  Case -5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleLongCollisionOrOp()
        {
            const string inputCode =
@"Sub Foo(x As Long)
Select Case x Or x < 5
  Case True
    'OK
  Case False 
    'OK
  Case -5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleLongCollisionConstantEvaluation()
        {
            const string inputCode =
@"

private const BASE As Long = 10
private const MAX As Long = BASE ^ 2

Sub Foo(x As Long)

Select Case x
  Case 100
    'OK
  Case MAX 
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleLongCollisionAndOp()
        {
            const string inputCode =
@"Sub Foo(x As Long)
Select Case x And x < 5
  Case True
    'OK
  Case False 
    'OK
  Case -5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_MixedSelectCaseTypes()
        {
            const string inputCode =
@"

private const MAXValue As Long = 5
private const TwentyFiveCents As Double = .25
private const MINCoins As Long = 4

Sub Foo(numQuarters As Byte)

Select Case numQuarters * TwentyFiveCents
  Case 1.25 To 10.00
    'OK
  Case MAXValue 
    'Unreachable
  Case MINCoins * TwentyFiveCents
    'OK
  Case MINCoins * 2
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleLongCollisionXOROp()
        {
            const string inputCode =
@"Sub Foo(x As Long)
Select Case x = 1 Xor x < 5
  Case True
    'OK
  Case False 
    'OK
  Case -5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleLongCollisionEqvOp()
        {
            const string inputCode =
@"Sub Foo(x As Long)
Select Case x Eqv 1
  Case True
    'OK
  Case False 
    'OK
  Case -5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleLongCollisionLogicalNotOp()
        {
            const string inputCode =
@"Sub Foo(x As Long)
Select Case Not x
  Case True
    'OK
  Case False 
    'OK
  Case -5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleLongCollisionLogicalModOp()
        {
            const string inputCode =
@"Sub Foo(x As Long)

private const ZeroMod As Long = 10
Select Case x
  Case ZeroMod Mod 2
    'OK
  Case 0 
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_ParensAroundSelectCase()
        {
            const string inputCode =
@"Sub Foo(x As Long)
Select Case (x)
  Case 1,2,-5
    'OK
  Case 2
    'Unreachable
  Case -5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_ExceedsIntegerValue()
        {
            const string inputCode =
@"Sub Foo(x As Integer)

Select Case x
  Case 40000
    'Unreachabel - Exceeds Integer value
  Case -50000
    'Unreachabel - Exceeds Integer value
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_ExceedsIntegerButIncludesAccessibleValues()
        {
            const string inputCode =
@"Sub Foo(x As Integer)

Select Case x
  Case 10,11,12
    'OK
  Case 15, 40000
    'Exceeds Integer value - but other value makes case reachable....no Error
  Case x < 4
    'OK
  Case -50000
    'Exceeds Integer values
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_ExceedsLongValue()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case 98
    'OK
  Case 5 To 25, 50, 80
    'OK
  Case 214#
    'OK
  Case 2147486648#
    'Unreachable
  Case -2147486649#
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IntegerWithDoubleValue()
        {
            const string inputCode =
@"Sub Foo(x As Integer)

Select Case x
  Case 214.0
    'OK - ish
  Case -214#
    'OK
  Case Is < -5000
    'OK
  Case 98
    'OK
  Case 5 To 25, 50, 80
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_ExceedsCurrencyValue()
        {
            const string inputCode =
@"Sub Foo(x As Currency)

Select Case x
  Case 85.5
    'OK
  Case -922337203685477.5809
    'Unreachable
  Case 922337203685490.5808
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_ExceedsSingleValue()
        {
            const string inputCode =
@"Sub Foo(x As Single)

Select Case x
  Case 85.5
    'OK
  Case -3402824E38
    'Unreachable
  Case 3402824E38
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_NumbersAsBooleanCases()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case x
  Case -5
    'Evaluates as 'True'
  Case 4
    'Unreachable
  Case 1
    'Unreachable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_HandlesBooleanConstants()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case x
  Case True
    'OK
  Case False
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanUnreachableCaseElse()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case x
  Case -7
    'OK
  Case False
    'OK
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanUnreachableCaseElseIsStmt()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case x
  Case Is > -40
    'OK
  Case False
    'unreachable
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanUnreachableCaseElseIsStmtGTAndValue()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case x
  Case Is > -1
    'OK
  Case -10
    'OK
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanUnreachableCaseElseIsStmtLTAndValue()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case x
  Case Is < 0
    'OK
  Case 0
    'OK
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanUnreachableCaseElseRange()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case x
  Case 0 To 10
    'OK
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanUnreachableCaseElseValueRange()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case x
  Case -10 To 5
    'OK
  Case False
    'unreachable
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanExpressionUnreachableCaseElseUsesLong()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case VBA.Rnd() > 0.5
  Case -7
    'OK
  Case False
    'OK
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_VariantSelectCase()
        {
            const string inputCode =
@"Sub Foo( x As Variant)

Select Case x
  Case .4 To .9
    'OK
  Case 0.23
    'OK
  Case 0.55
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_VariantSelectCaseInferFromConstant()
        {
            const string inputCode =
@"Sub Foo( x As Variant)

private Const TheValue As Double = 45.678
private Const TheUnreachableValue As Long = 25

Select Case x
  Case TheValue * 2
    'OK
  Case 0 To TheValue
    'OK
  Case TheUnreachableValue
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_VariantSelectCaseInferFromConstant2()
        {
            const string inputCode =
@"Sub Foo( x As Variant)

private Const TheValue As Double = 45.678
private Const TheUnreachableValue As Long = 77

Select Case x
  Case x > TheValue
    'OK
  Case 0 To TheValue - 20
    'OK
  Case TheUnreachableValue
    'Unreachable
  Case 55
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BuiltInSelectCase()
        {
            const string inputCode =
@"Sub Foo( x As Variant)

Select Case VBA.Rnd()
  Case .4 To .9
    'OK
  Case 0.23
    'OK
  Case 0.55
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanNEQ()
        {
            const string inputCode =
@"Sub Foo( x As Boolean)

Select Case x
  Case True
    'OK
  Case x <> False
    'Unreachable
  Case 95
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanNEQCaseElse()
        {
            const string inputCode =
@"Sub Foo( x As Boolean)

Select Case x
  Case 95
    'OK
  Case x <> False
    'unreachable
  Case 100
    'unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_LongCollisionIndeterminateCase()
        {
            const string inputCode =
@"Sub Foo( x As Long, y As Double)

Select Case x
  Case x > -3000
    'OK
  Case x < y
    'OK - indeterminant
  Case 95
    'Unreachable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_LongCollisionMultipleVariablesSameType()
        {
            const string inputCode =
@"Sub Foo( x As Long, y As Long)

Select Case x * y
  Case x > -3000
    'OK
  Case y > -3000
    'OK
  Case x < y
    'OK - indeterminant
  Case 95
    'OK - this gives a false positive when evaluated as if 'x' or 'y' is the only select case variable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_LongCollisionMultipleVariablesDifferentType()
        {
            const string inputCode =
@"Sub Foo( x As Long, y As Double)

Select Case x * y
  Case x > -3000
    'OK
  Case y > -3000
    'OK
  Case x < y
    'OK - indeterminant
  Case 95
    'OK - this gives a false positive when evaluated as if 'x' or 'y' is the only select case variable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_LongCollisionVariableAndConstantDifferentType()
        {
            const string inputCode =
@"Sub Foo( x As Long)

private const y As Double = 0.5

Select Case x * y
  Case x > -3000
    'OK
  Case y > -3000
    'Unreachable
  Case x < y
    'OK - indeterminant
  Case 95
    'OK - this gives a false positive when evaluated as if 'x' is the only select case variable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_LongCollisionUnaryMathOperation()
        {
            const string inputCode =
@"Sub Foo( x As Long, y As Double)

Select Case -x  'math on the Select Case variable disqualifies inspection
  Case x > -3000
    'OK
  Case y > -3000
    'OK
  Case x < y
    'OK - indeterminant
  Case 95
    'unreachable - not evaluated
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanExpressionUnreachableCaseElseRangeClauseLong()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case x
  Case -5 To 5
    'OK
  Case True
    'Unreachable
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanExpressionUnreachableCaseElseUsingBoolean()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case VBA.Rnd() > 0.5
  Case True To False
    'OK
  Case True
    'Unreachable
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_BooleanExpressionUnreachableCaseElseInvertBooleanRange()
        {
            const string inputCode =
@"Sub Foo(x As Boolean)

Select Case VBA.Rnd() > 0.5
  Case False To True 
    'OK
  Case True
    'Unreachable
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_ExceedsByteValue()
        {
            const string inputCode =
@"Sub Foo(x As Byte)

Select Case x
  Case 2,5,7
    'OK
  Case 254
    'OK
  Case 255
    'OK
  Case 256
    'Out of Range
  Case -1
    'Out of Range
  Case 0
    'OK
  Case 1
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleDoubleCollision()
        {
            const string inputCode =
@"Sub Foo(x As Double)

Select Case x
  Case 4.4, 4.5, 4.6
    'OK
  Case 4.5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SimpleIntegerCollision()
        {
            const string inputCode =
@"Sub Foo(x As Integer)

Select Case x
  Case 1,2,5
    'OK
  Case 2
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_StringWhereLongShouldBe()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case 1 To 49
    'OK
  Case 50
    'OK
  Case ""Test""
    'Unreachable
  Case ""85""
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, mismatch: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_MixedTypes()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case 1 To 49
    'OK
  Case ""Test"", 100, ""92""
    'OK - ""Test"" will not be evaluated
  Case ""85""
    'OK
  Case 2
    'Unreachable
  Case 92
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_StringWhereLongShouldBeIncludeLongAsString()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case 1 To 49
    'OK
  Case ""51""
    'OK
  Case ""Hello World""
    'Unreachable
  Case 50
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, mismatch: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_DoubleWhereLongShouldBe()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case 88.55
    'OK - but may not get what you expect
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_MultipleRanges()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
  Case 1 To 4, 7 To 9, 11, 13, 15 To 20
    'OK
  Case 8
    'Unreachable
  Case 11
    'Unreachable
  Case 17
    'Unreachable
  Case 21
    'Reachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 3);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CascadingIsStatements()
        {
            const string inputCode =
@"Sub Foo(LNumber As Long)

Select Case LNumber
   Case Is < 100
      'OK
   Case Is < 200
      'OK
   Case Is < 300
      'OK
   Case Else
      'OK
   End Select
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CascadingIsStatementsGT()
        {
            const string inputCode =
@"Sub Foo(LNumber As Long)

Select Case LNumber
   Case Is > 300
    'OK
   Case Is > 200
    'OK  
   Case Is > 100
    'OK  
   Case Else
    'OK
   End Select
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStatementUnreachableGT()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
   Case Is > 100
       'OK  
   Case Is > 200
      'unreachable  
   Case Is > 300
      'unreachable
  Case Else
      'OK
   End Select
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStatementUnreachableLT()
        {
            const string inputCode =
@"Sub Foo(x As Long)

Select Case x
   Case Is < 300
       'OK  
   Case Is < 200
      'unreachable  
   Case Is < 100
      'unreachable
  Case Else
      'OK
   End Select
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtGT()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is > 5000
    'OK
  Case 5000
    'OK
  Case 5001
    'Unreachable
  Case 10000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtToIsStmtCaseElseUnreachable()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is > 5000 
    'OK
  Case Is < 10000
    'OK
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtToIsStmtCaseElseUnreachableLTE()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is > 5000 
    'OK
  Case Is <= 10000
    'OK
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtToIsStmtCaseElseUnreachableUsingIs()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case z <> 5 
    'OK
  Case Is = 5
    'OK
  Case 400
    'Unreachable
  Case Else
    'Unreachable
End Select
End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1,  caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseClauseHasParens()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

private const maxValue As Long = 5000
private const subtract As Long = 2000

Select Case z
  Case (maxValue - subtract) * 10
    'OK
  Case 30000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseClauseHasMultipleParens()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

private const maxValue As Long = 5000
private const subtractValue As Long = 2000

Select Case z
  Case (maxValue - subtractValue) * (55 - 35) / 10
    'OK
  Case 6000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_RelationalOpSimple()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case z > 5000
    'OK
  Case 5000
    'OK
  Case 5001
    'Unreachable
  Case 10000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test] 
        [Category("Inspections")]
        public void UnreachableCaseInspection_SelectCaseHasMultOpWithFunction()
        {
            const string inputCode =
@"
Function Bar() As Long
    Bar = 5
End Function

Sub Foo(z As Long)

Select Case Bar() * z
  Case Is > 5000
    'OK
  Case 5000
    'OK
  Case 5001
    'Unreachable
  Case 10000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseClauseHasAddOp()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

private const maxValue As Long = 5000

Select Case z
  Case maxValue - 1000
    'OK
  Case 4000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseClauseHasAddOpWithConstants()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

private const maxValue As Long = 5000
private const adder As Long = 3500

Select Case z
  Case maxValue + adder
    'OK
  Case 8500
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseClauseHasMultOp()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

private const maxValue As Long = 5000

Select Case z
  Case 2 * maxValue
    'OK
  Case 10000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseClauseHasMultOpInParens()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

private const maxValue As Long = 5000

Select Case (((z)))
  Case 2 * maxValue
    'OK
  Case 10000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseClauseHasExpOp()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

private const maxPower As Long = 3

Select Case z
  Case 2 ^ maxPower
    'OK
  Case 8
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseClauseHasMultOp2Constants()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

private const maxValue As Long = 5000
private const minMultiplier As Long = 2

Select Case z
  Case maxValue / minMultiplier
    'OK
  Case 2500
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseClauseHasMultOp2Literals()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

Select Case z
  Case 5000 / 2
    'OK
  Case 2500
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SelectCaseUsesConstant()
        {
            const string inputCode =
@"
private Const maxValue As Long = 5000

Sub Foo(z As Long)

Select Case z
  Case Is > maxValue
    'OK
  Case 6000
    'Unreachable
  Case 8500
    'Unreachable
  Case 15
    'OK
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EnumerationNumberRangeNoDetection()
        {
            const string inputCode =
@"
private Enum Weekday
    Sunday = 1
    Monday = 2
    Tuesday = 3
    Wednesday = 4
    Thursday = 5
    Friday = 6
    Saturday = 7
 End Enum

Sub Foo(z As Weekday)

Select Case z
  Case Weekday.Monday To Weekday.Saturday
    'OK
  Case z = Weekday.Tuesday
    'Unreachable
  Case Weekday.Wednesday
    'Unreachable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EnumerationNumberRange()
        {
            const string inputCode =
@"
private Enum Weekday
    Sunday = 1
    Monday = 2
    Tuesday = 3
    Wednesday = 4
    Thursday = 5
    Friday = 6
    Saturday = 7
 End Enum

Sub Foo(z As Weekday)

Select Case z
  Case Weekday.Monday To Weekday.Saturday
    'OK
  Case z = Weekday.Tuesday
    'Unreachable
  Case Weekday.Wednesday
    'Unreachable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EnumerationNumberRangeNonConstant()
        {
            const string inputCode =
@"
private Enum BitCountMaxValues
    max1Bit = 2 ^ 0
    max2Bits = 2 ^ 1 + max1Bit
    max3Bits = 2 ^ 2 + max2Bits
    max4Bits = 2 ^ 3 + max3Bits
End Enum

Sub Foo(z As BitCountMaxValues)

Select Case z
  Case 7
    'OK
  Case BitCountMaxValues.max3Bits
    'Unreachable
  Case BitCountMaxValues.max4Bits
    'OK
  Case 15
    'Unreachable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EnumerationNumberRangeInvalidValue()
        {
            const string inputCode =
@"
private Enum BitCountMaxValues
    max1Bit = 2 ^ 0
    max2Bits = 2 ^ 1 + max1Bit
    max3Bits = 2 ^ 2 + max2Bits
    max4Bits = 2 ^ 3 + max3Bits
End Enum

Sub Foo(z As BitCountMaxValues)

Select Case z
  Case 12
    'Unreachable
  Case BitCountMaxValues.max3Bits
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EnumerationNumberRangeConflicts()
        {
            const string inputCode =
@"
private Enum Fruit
    Apple = 10
    Pear = 20
    Orange = 30
 End Enum

Sub Foo(z As Fruit)

Select Case z
  Case -4 To 11
    'OK - captures 'Apple' only
  Case 10 To 16 
    'Unreachable - The only captured enum 'Apple' is already handled by the first case
  Case 11 To 19
    'Unreachable - no valid enums fall in this range
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EnumerationNumberRangeIsStmtConflictLT()
        {
            const string inputCode =
@"
private Enum Fruit
    Apple = 10
    Pear = 20
    Orange = 30
 End Enum

Sub Foo(z As Fruit)

Select Case z
  Case 10 To 25
    'OK
  Case z < 21 
    'Unreachable - all enums < 21 have already been captured
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_EnumerationNumberRangeIsStmtConflictGTE()
        {
            const string inputCode =
@"
private Enum Fruit
    Apple = 10
    Pear = 20
    Orange = 30
 End Enum

Sub Foo(z As Fruit)

Select Case z
  Case 25 To 30
    'OK
  Case z >= 30 
    'Unreachable - all enums >= 30 have already been captured
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_RangeCollisionsWithEnums()
        {
            const string inputCode =
@"
private Enum Fruit
    Apple = 10
    Pear = 20
    Orange = 30
 End Enum

Sub Foo(z As Long)

Select Case z
  Case z >= Orange
    'OK
  Case 30 To 100
    'Unreachable
  Case Pear
    'OK
  Case 20
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SelectCaseUsesConstantReferenceExpr()
        {
            const string inputCode =
@"
private Const maxValue As Long = 5000

Sub Foo(z As Long)

Select Case ( z * 3 ) - 2   'math on the Select Case variable disqualifies inspection
  Case z > maxValue
    'OK
  Case 15
    'OK
  Case 6000
    'Unreachable - not evaluated
  Case 8500
    'Unreachable - not evaluated
   Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 0);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SelectCaseUsesConstantReferenceOnRHS()
        {
            const string inputCode =
@"
private Const maxValue As Long = 5000

Sub Foo(z As Long)

Select Case z
  Case maxValue < z
    'OK
  Case 6000
    'Unreachable
  Case 15
    'OK
  Case 8500
    'Unreachable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SelectCaseUsesConstantIndeterminantExpression()
        {
            const string inputCode =
@"
private Const maxValue As Long = 5000

Sub Foo(z As Long)

Select Case z
  Case z > maxValue / 2
    'OK
  Case z > maxValue
    'Unreachable
  Case 15
    'OK
  Case 8500
    'Unreachable
  Case Else
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SelectCaseIsFunction()
        {
            const string inputCode =
@"
Function Bar() As Long
    Bar = 5
End Function

Sub Foo()

Select Case Bar()
  Case Is > 5000
    'OK
  Case 5000
    'OK
  Case 5001
    'Unreachable
  Case 10000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_SelectCaseIsFunctionWithParams()
        {
            const string inputCode =
@"
Function Bar(x As Long, y As Double) As Long
    Bar = 5
End Function

Sub Foo(firstVar As Long, secondVar As Double)

Select Case Bar( firstVar, secondVar )
  Case Is > 5000
    'OK
  Case 5000
    'OK
  Case 5001
    'Unreachable
  Case 10000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_RelationalOpExpression()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case 500 < z
    'OK
  'Case 500
    'OK
  Case 501
    'Unreachable
  Case 1000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtGTFollowsRange()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case 3 To 10
    'OK
  Case Is > 8
    'OK
  Case 4
    'Unreachable
  Case 2
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtGTConflicts()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case 5000
    'OK
  Case 10000
    'OK
  Case Is > 5000
    'OK
  Case 5001
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtGTE()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is >= 5000
    'OK
  Case 4999
    'OK
  Case 5000
    'Unreachable
  Case 10000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtLT()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is < 5000
    'OK
  Case 7
    'Unreachable
  Case 4999
    'Unreachable
  Case 5000
    'OK
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtLTE()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is <= 5000
    'OK
  Case 7
    'Unreachable
  Case 5001
    'OK
  Case 5000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtLTEReverse()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case 7
    'OK
  Case Is <= 5000
    'OK
  Case 5001
    'OK
  Case 5000
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtNEQ()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is <> 8
    'OK
  Case 7
    'Unreachable
  Case 5001
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtNEQReverseOrder()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case 7
    'OK
  Case Is <> 8
    'OK
  Case 5001
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_CaseElseIsStmtNEQAndSingleValue()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case 8
    'OK
  Case Is <> 8
    'OK
  Case -4000
    'Unreachable
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtNEQAllValues()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is <> 8
    'OK
  Case 8
    'OK
  Case 5001
    'Unreachable
  Case Else
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1, caseElse: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtEQ()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is = 8
    'OK
  Case 8
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtEQReveserOrder()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case 8
    'OK
  Case Is = 8
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtMultiple()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is > 8
    'OK
  Case 8
    'OK
  Case  Is = 9
    'Unreachable
  Case Is < 100
    'OK
  Case Is < 5
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable:2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtAndNegativeRange()
        {
            const string inputCode =
@"Sub Foo(z As Long)

Select Case z
  Case Is < 8
    'OK
  Case -10 To -3
    'Unreachable
  Case 0
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_IsStmtAndNegativeRangeWithConstants()
        {
            const string inputCode =
@"
private const START As Long = 10
private const FINISH As Long = 3

Sub Foo(z As Long)
Select Case z
  Case Is < 8
    'OK
  Case -(START * 4) To -(FINISH * 2) 
    'Unreachable
  Case 0
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 2);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_TypeHintString()
        {
            const string inputCode =
@"
Sub Foo(z As String)

Dim SomeString$

SomeString$ = SomeString$ & z

Select Case SomeString$
  Case ""Here"" To ""Eternity""
    'OK
  Case ""Forever""
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_TypeHintDouble()
        {
            const string inputCode =
@"
Sub Foo(z As Double)

Dim SomeDouble#

SomeDouble# = SomeDouble# + z

Select Case SomeDouble#
  Case 10.00 To 30.00
    'OK
  Case 20.00
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_TypeHintSingle()
        {
            const string inputCode =
@"
Sub Foo(z As Single)

Dim SomeSingle!

SomeSingle! = SomeSingle! + z

Select Case SomeSingle!
  Case 10.00 To 30.00
    'OK
  Case 20.00
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_TypeHintInt()
        {
            const string inputCode =
@"
Sub Foo(z As Integer)

Dim SomeInteger$

SomeInteger% = SomeInteger% + z

Select Case SomeInteger%
  Case 10 To 30
    'OK
  Case 20
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        [Test]
        [Category("Inspections")]
        public void UnreachableCaseInspection_TypeHintLong()
        {
            const string inputCode =
@"
Sub Foo(z As Long)

Dim SomeLong&

SomeLong& = SomeLong& + z

Select Case SomeLong&
  Case 10 To 30
    'OK
  Case 20
    'Unreachable
End Select

End Sub";
            CheckActualResultsEqualsExpected(inputCode, unreachable: 1);
        }

        private void CheckActualResultsEqualsExpected(string inputCode, int unreachable = 0, int mismatch = 0, /*int outOfRange = 0,*/ int caseElse = 0)
        {
            var expected = new Dictionary<string, int>
            {
                { InspectionsUI.UnreachableCaseInspection_Unreachable, unreachable },
                { InspectionsUI.UnreachableCaseInspection_TypeMismatch, mismatch },
                { InspectionsUI.UnreachableCaseInspection_CaseElse, caseElse },
            };

            var vbe = MockVbeBuilder.BuildFromSingleStandardModule(inputCode, out var _);
            var state = MockParser.CreateAndParse(vbe.Object);

            var inspection = new UnreachableCaseInspection(state);
            var inspector = InspectionsHelper.GetInspector(inspection);
            var actualResults = inspector.FindIssuesAsync(state, CancellationToken.None).Result;

            var actualUnreachable = actualResults.Where(ar => ar.Description.Equals(InspectionsUI.UnreachableCaseInspection_Unreachable));
            var actualMismatches = actualResults.Where(ar => ar.Description.Equals(InspectionsUI.UnreachableCaseInspection_TypeMismatch));
            var actualUnreachableCaseElses = actualResults.Where(ar => ar.Description.Equals(InspectionsUI.UnreachableCaseInspection_CaseElse));

            Assert.AreEqual(expected[InspectionsUI.UnreachableCaseInspection_Unreachable], actualUnreachable.Count(), "Unreachable result");
            Assert.AreEqual(expected[InspectionsUI.UnreachableCaseInspection_TypeMismatch], actualMismatches.Count(), "Mismatch result");
            Assert.AreEqual(expected[InspectionsUI.UnreachableCaseInspection_CaseElse], actualUnreachableCaseElses.Count(), "CaseElse result");
        }
    }
}
