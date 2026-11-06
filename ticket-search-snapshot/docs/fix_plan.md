#24.10
_надо исправить отображения в json-е, когда некоторые параметры пишустя n раз_

_пока что ловит только сетевые ошибки_
`try:
        response = requests.get(url, params=params, headers=headers, timeout=10)
    except requests.exceptions.RequestException:
        return []
`
#ВОПРОС
моя задача сводится исключительно к подключению к апи или мне надо реализовать логику для сортировки билетов