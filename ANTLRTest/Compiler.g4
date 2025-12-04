grammar Compiler;

@header {
namespace ANTLRTest;
}

options {
    language=CSharp;
}

program: (statement)* EOF;

varType: INT_TYPE | DOUBLE_TYPE | FLOAT_TYPE | STRING_TYPE;
numericType: INT_TYPE | DOUBLE_TYPE | FLOAT_TYPE;
stringType: STRING_TYPE ;
statement: semiCol
        | varDeclaration
        | varAssignment semiCol
        | stringType ID (ASSIGN string)? semiCol
        | arithmExpr semiCol
        | ifStatement
        | forStatement
        | whileStatement
        | returnStatement
         ;
fragment DIGIT: [0-9] ;
fragment NON_ZERO_DIGIT: [1-9] ;
fragment ZERO: '0' ;
fragment CHAR: [a-zA-Z_] ;

ifStatement: IF_TOKEN LPAREN logicalExpr RPAREN LBRACE (statement)* RBRACE (ELSE_TOKEN LBRACE (statement)* RBRACE)? ;
forStatement: FOR_TOKEN LPAREN varDeclaration? logicalExpr? semiCol varAssignment? RPAREN LBRACE (statement)* RBRACE ;
whileStatement: WHILE_TOKEN LPAREN logicalExpr? RPAREN LBRACE (statement)* RBRACE ;
returnStatement: RETURN_TOKEN arithmExpr? semiCol ;
number: DOUBLETOKEN | INTTOKEN ;
string: STRINGTOKEN ;

singleLogicalExpr: arithmExpr ( comparators arithmExpr )? 
    | LPAREN logicalExpr RPAREN
    | UNARY_LOGICAL_OP singleLogicalExpr
    | ID comparators string
    ;
logicalExpr: singleLogicalExpr ( logicalOp logicalExpr )* ;

semiCol: ';' ;

varDeclaration: numericType (ID (ASSIGN arithmExpr)? (COMMA ID (ASSIGN arithmExpr)?)*) semiCol;
varAssignment: ID ASSIGN varValue
    | ID INCR
    | ID DECR
    | ID PLUS_EQ varValue
    | ID MINUS_EQ varValue
    | ID MUL_EQ varValue
    | ID DIV_EQ varValue
    | ID MOD_EQ varValue
    ;

varValue: arithmExpr | string ;

arithmExpr:
      arithmExpr op=( MUL | DIV ) arithmExpr        # MulDivExpr
    | arithmExpr op=( PLUS | MINUS ) arithmExpr     # AddSubExpr
    | atom                                          # AtomExpr
    ;

atom:
    number                    # NumberAtom
    | ID                        # IdAtom
    | '(' arithmExpr ')'          # ParenExpr
    ;

term
    : factor (arithmOperation factor)*
    ;

arithmOperation
    : PLUS
    | MINUS
    | MUL
    | DIV
    | MOD
    ;

factor:
    INTTOKEN
    | DOUBLETOKEN
    | FLOATTOKEN
    | ID
    | LPAREN arithmExpr RPAREN
    ;

comparators: LT | LEQ | GT | GEQ | EQ | NEQ ;
logicalOp: BINARY_LOGICAL_OP | UNARY_LOGICAL_OP ;

COMMENT: '//' ~[\r\n]* -> skip ;
IF_TOKEN: 'if' ;
ELSE_TOKEN: 'else' ;
FOR_TOKEN: 'for' ;
WHILE_TOKEN: 'while' ;
RETURN_TOKEN: 'return' ;
INTTOKEN: ZERO | NON_ZERO_DIGIT DIGIT* ;
DOUBLETOKEN: INTTOKEN '.' DIGIT+ ;
FLOATTOKEN: DOUBLETOKEN ;
STRINGTOKEN: DQUOTE (~["\r\n])* DQUOTE ;
WS: [ \t\r\n]+ -> skip ;

INT_TYPE: 'int' ;
DOUBLE_TYPE: 'double' ;
FLOAT_TYPE: 'float' ;
STRING_TYPE: 'string' ;
PLUS: '+' ;
MINUS: '-' ;
MUL: '*' ;
DIV: '/' ;
MOD: '%' ;
ASSIGN: '=' ;
INCR: '++' ;
DECR: '--' ;
PLUS_EQ: '+=' ;
MINUS_EQ: '-=' ;
MUL_EQ: '*=' ;
DIV_EQ: '/=' ;
MOD_EQ: '%=' ;

LPAREN: '(' ;
RPAREN: ')' ;
SEMI: ';' ;
COMMA: ',' ;
DOT: '.' ;
LBRACE: '{' ;
RBRACE: '}' ;
LBRACK: '[' ;
RBRACK: ']' ;
LT: '<' ;
LEQ: '<=' ;
GT: '>' ;
GEQ: '>=' ;
EQ: '==' ;
NEQ: '!=' ;
DQUOTE: '"' ;
ID: [a-zA-Z_][a-zA-Z0-9_]* ;
BINARY_LOGICAL_OP: '&&' | '||';
UNARY_LOGICAL_OP: '!' ;