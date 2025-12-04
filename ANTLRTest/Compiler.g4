grammar Compiler;

@header {
namespace ANTLRTest;
}

options {
	language = CSharp;
}

program: statement* mainFunction (statement)* EOF;

varType: INT_TYPE | DOUBLE_TYPE | FLOAT_TYPE | STRING_TYPE;
numericType: INT_TYPE | DOUBLE_TYPE | FLOAT_TYPE;
stringType: STRING_TYPE;
statement:
	SEMICOL
	| varDeclaration SEMICOL
	| varAssignment SEMICOL
	| arithmExpr SEMICOL
	| ifStatement
	| forStatement
	| whileStatement
	| returnStatement
	| functionCall SEMICOL;
fragment DIGIT: [0-9];
fragment NON_ZERO_DIGIT: [1-9];
fragment ZERO: '0';
fragment CHAR: [a-zA-Z_];

ifStatement:
	IF_TOKEN LPAREN logicalExpr RPAREN LBRACE (statement)* RBRACE (
		ELSE_TOKEN LBRACE (statement)* RBRACE
	)?;
forStatement:
	FOR_TOKEN LPAREN varDeclaration? SEMICOL logicalExpr? SEMICOL varAssignment? RPAREN LBRACE (
		statement
	)* RBRACE;
whileStatement:
	WHILE_TOKEN LPAREN logicalExpr? RPAREN LBRACE (statement)* RBRACE;
returnStatement: RETURN_TOKEN expression? SEMICOL;
expression: arithmExpr | string;
number: FLOAT_DOUBLE_TOKEN | INT_TOKEN;
string: STRING_TOKEN;

singleLogicalExpr:
	arithmExpr (comparators arithmExpr)?
	| LPAREN logicalExpr RPAREN
	| UNARY_LOGICAL_OP singleLogicalExpr
	| ID comparators string
	| TRUE
	| FALSE
	| NULL;
logicalExpr: (singleLogicalExpr ( BINARY_LOGICAL_OP logicalExpr)*);

varDeclaration:
	varType (
		ID (ASSIGN (arithmExpr | string))? (
			COMMA ID (ASSIGN (arithmExpr | string))?
		)*
	);
varAssignment:
	ID ASSIGN varValue
	| ID INCR
	| ID DECR
	| ID PLUS_EQ varValue
	| ID MINUS_EQ varValue
	| ID MUL_EQ varValue
	| ID DIV_EQ varValue
	| ID MOD_EQ varValue;

varValue: arithmExpr | string;

arithmExpr:
	arithmExpr op = (MUL | DIV | MOD) arithmExpr	# MulDivExpr
	| arithmExpr op = (PLUS | MINUS) arithmExpr		# AddSubExpr
	| atom											# AtomExpr
	| functionCall									# FunctionCallExpr;

atom:
	number					# NumberAtom
	| ID					# IdAtom
	| '(' arithmExpr ')'	# ParenExpr;

arithmOperation: PLUS | MINUS | MUL | DIV | MOD;

comparators: LT | LEQ | GT | GEQ | EQ | NEQ;
logicalOp: BINARY_LOGICAL_OP | UNARY_LOGICAL_OP;

BUILTIN_FUNC: SQRT | LOG | SIN | COS;
functionCall: (ID | BUILTIN_FUNC) LPAREN (
		expression (COMMA expression)*
	)? RPAREN;
mainFunction:
	INT_TYPE MAIN LPAREN RPAREN LBRACE (statement)* RBRACE;

SINGLE_LINE_COMMENT: '//' ~[\r\n]* -> skip;
MULTI_LINE_COMMENT: '/*' .*? '*/' -> skip;
COMMENT: SINGLE_LINE_COMMENT | MULTI_LINE_COMMENT;
IF_TOKEN: 'if';
ELSE_TOKEN: 'else';
FOR_TOKEN: 'for';
WHILE_TOKEN: 'while';
RETURN_TOKEN: 'return';
FLOAT_DOUBLE_TOKEN: INT_TOKEN '.' DIGIT+;
INT_TOKEN: ZERO | NON_ZERO_DIGIT DIGIT*;
STRING_TOKEN: DQUOTE (~["\r\n])* DQUOTE;
WS: [ \t\r\n]+ -> skip;

INT_TYPE: 'int';
DOUBLE_TYPE: 'double';
FLOAT_TYPE: 'float';
STRING_TYPE: 'string';
PLUS: '+';
MINUS: '-';
MUL: '*';
DIV: '/';
MOD: '%';
ASSIGN: '=';
INCR: '++';
DECR: '--';
PLUS_EQ: '+=';
MINUS_EQ: '-=';
MUL_EQ: '*=';
DIV_EQ: '/=';
MOD_EQ: '%=';

LPAREN: '(';
RPAREN: ')';
SEMICOL: ';';
COMMA: ',';
DOT: '.';
LBRACE: '{';
RBRACE: '}';
LBRACK: '[';
RBRACK: ']';
LT: '<';
LEQ: '<=';
GT: '>';
GEQ: '>=';
EQ: '==';
NEQ: '!=';
DQUOTE: '"';
BINARY_LOGICAL_OP: '&&' | '||';
UNARY_LOGICAL_OP: '!';
TRUE: 'true';
FALSE: 'false';
NULL: 'null';

MAIN: 'main';
SQRT: 'sqrt';
LOG: 'log';
SIN: 'sin';
COS: 'cos';

ID: [a-zA-Z_][a-zA-Z0-9_]*;