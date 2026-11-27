"""Ajudantes para listas de objetos devolvidas pela API."""
from robot.api.deco import keyword, library


@library(scope="GLOBAL")
class Dados:
    @keyword
    def item_com(self, lista, campo, valor):
        """Primeiro item de ``lista`` em que ``item[campo] == valor`` (falha se não houver)."""
        for item in lista:
            if item.get(campo) == valor:
                return item
        raise AssertionError(f"Nenhum item com {campo} = {valor!r} em {len(lista)} item(ns).")

    @keyword
    def item_com_campos(self, lista, **campos):
        """Primeiro item em que todos os ``campos`` têm os valores informados."""
        for item in lista:
            if all(item.get(c) == v for c, v in campos.items()):
                return item
        raise AssertionError(f"Nenhum item com {campos} em {len(lista)} item(ns).")

    @keyword
    def existe_item(self, lista, campo, valor, campo_texto, trecho):
        """True se algum item tem ``item[campo] == valor`` e ``trecho`` contido em ``item[campo_texto]``."""
        return any(i.get(campo) == valor and trecho in (i.get(campo_texto) or "") for i in lista)

    @keyword
    def valores_do_campo(self, lista, campo):
        return [item.get(campo) for item in lista]
