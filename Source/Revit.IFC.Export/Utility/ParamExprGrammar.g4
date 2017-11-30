/*
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2017  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
*/

grammar ParamExprGrammar;
@header {
#pragma warning disable 3021
}

@lexer::members
{
	public static int WHITESPACE = 1;
	public static int COMMENTS = 2;
}

/*
 * Parser Rules
 */
param_expr:       '{' expr '}' | '[Uu]' '{' expr '}';
expr:               value
                  | atomic_param
                  | unary_operator expr
                  | expr ops expr
                  | '(' expr ')' (power_op)?
                  ;
atomic_param:     objref param_name ('.' param_name)          // Support only 1 level nested reference, no need for overly complex reference
                  | special_param;
objref:           THIS | TYPE ;
type:             TYPE;
special_param:    ELEMENTID
                  | RUNNINGNUMBER 
                  | RUNNINGNUMBERINSTANCE ;
//                  | AUTOCALCULATE ;
param_name:       name | type name;
//name:             '(' (ESC | NAMEWITHSPECIALCHAR)+ ')' ;
name:             '(' STRING ')' ;
unary_operator:   '+' | '-' ;
ops:              MULTIPLY | DIVIDE | ADDITION | SUBTRACT ;
power_op:         '^' ( '-' | '+' )? INT;
value:              realliteral | stringliteral ;
stringliteral:		STRING ;
realliteral:		signed_number | UNITTYPEENUM '(' signed_number ')';
signed_number:	   ( '+' | '-' )? NUMBER ;

/*
 * Lexer rules
*/
THIS:               '$'[Tt][Hh][Ii][Ss];
TYPE:               '$'[Tt][Yy][Pp][Ee];
ELEMENTID:          '$'[Ee][Ll][Ee][Mm][Ee][Nn][Tt][Ii][Dd];
RUNNINGNUMBER:      '#';
RUNNINGNUMBERINSTANCE:  '##';
AUTOCALCULATE:      '$'[Aa][Uu][Tt][Oo] | '$'[Aa][Uu][Tt][Oo][Mm][Aa][Tt][Ii][Cc] ;
UNITTYPEENUM:       [Uu][Tt] '_' ALPHANUMERIC+ ;

/* Operators */
MULTIPLY:		'*';
DIVIDE:			'/';
ADDITION:		'+';
SUBTRACT:		'-';

STRING :		(['] (ESC | .)*? ['])
                        | (["] (ESC | .)*? ["]);
NUMBER:			INT '.' INT? EXP?   // 1.35, 1.35E-9, 0.3
				   | '.' INT EXP?			// .2, .2e-9
				   | INT EXP?            // 1e10
				   | INT                // 45
				   ;
fragment ALPHANUMERIC:          [a-zA-Z0-9_] ;
fragment ESC:			'\\' (["\\/bfnrt] | UNICODE) ;
fragment UNICODE :		'u' HEX HEX HEX HEX ;
fragment HEX :			[0-9a-fA-F] ;
fragment NAMEWITHSPECIALCHAR:   [a-zA-Z0-9&*%^@!_=+-/.,];
fragment INT:                   [0] | [0-9] [0-9]* ; 
fragment EXP:                   [Ee] [+\-]? INT ; 

WS:				[ \t\n\r]+ -> channel(WHITESPACE) ;